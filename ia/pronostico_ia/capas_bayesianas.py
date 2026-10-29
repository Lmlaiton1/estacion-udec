"""LSTM variacional para pronóstico categórico multihorizonte.

Todos los kernels y sesgos de la LSTM y de sus cabezas tienen posteriores
Normal(mu, softplus(rho)). Se muestrean una vez por llamada y se reutilizan
durante todos los pasos de la secuencia. La inferencia es variacional,
aproximada; los intervalos Monte Carlo describen variación de probabilidades,
no garantizan la cobertura de los eventos meteorológicos.
"""
from __future__ import annotations

import os
from collections.abc import Mapping

os.environ.setdefault("TF_CPP_MIN_LOG_LEVEL", "2")
os.environ.setdefault("TF_NUM_INTRAOP_THREADS", "2")
os.environ.setdefault("TF_NUM_INTEROP_THREADS", "2")

import numpy as np
import tensorflow as tf
from tensorflow import keras


def _normal_kl(mu, rho, prior_sd):
    """KL(q || p), con q Normal diagonal y p Normal(0, prior_sd)."""
    sd = tf.nn.softplus(rho) + tf.cast(1e-6, rho.dtype)
    prior = tf.cast(prior_sd, mu.dtype)
    return tf.reduce_sum(
        tf.math.log(prior / sd)
        + (tf.square(sd) + tf.square(mu)) / (2.0 * tf.square(prior))
        - 0.5
    )


def _draw_normal(mu, rho, training):
    def sample():
        sd = tf.nn.softplus(rho) + tf.cast(1e-6, rho.dtype)
        return mu + sd * tf.random.normal(tf.shape(mu), dtype=mu.dtype)

    if training is None or training is False:
        return mu
    if training is True:
        return sample()
    return tf.cond(tf.cast(training, tf.bool), sample, lambda: tf.identity(mu))


@keras.utils.register_keras_serializable(package="ClimaBayesiano")
class BayesianLSTM(keras.layers.Layer):
    """LSTM con posterior variacional explícito sobre todos sus pesos.

    Una llamada representa un único conjunto de pesos (compartido por los
    ejemplos del lote). Nunca se vuelve a muestrear un kernel dentro del bucle
    temporal. `training=True` muestrea y `False` usa las medias posteriores.
    """

    def __init__(self, units=24, n_train=1, prior_sd=1.0,
                 posterior_rho_init=-4.0, seed=42, **kwargs):
        super().__init__(**kwargs)
        self.units = int(units)
        self.n_train = int(n_train)
        self.prior_sd = float(prior_sd)
        self.posterior_rho_init = float(posterior_rho_init)
        self.seed = int(seed)
        if self.units < 1 or self.n_train < 1 or self.prior_sd <= 0:
            raise ValueError("units y n_train deben ser positivos; prior_sd > 0.")
        self.input_spec = keras.layers.InputSpec(ndim=3)

    def build(self, input_shape):
        n_features = int(input_shape[-1])
        shapes = {
            "kernel": (n_features, 4 * self.units),
            "recurrent_kernel": (self.units, 4 * self.units),
            "bias": (4 * self.units,),
        }
        initializers = {
            "kernel": keras.initializers.GlorotUniform(seed=self.seed),
            "recurrent_kernel": keras.initializers.Orthogonal(seed=self.seed + 1),
            "bias": keras.initializers.Constant(
                np.concatenate([
                    np.zeros(self.units), np.ones(self.units),
                    np.zeros(2 * self.units),
                ]).astype("float32")
            ),
        }
        for name, shape in shapes.items():
            setattr(self, name + "_mu", self.add_weight(
                name=name + "_mu", shape=shape, initializer=initializers[name],
                trainable=True,
            ))
            setattr(self, name + "_rho", self.add_weight(
                name=name + "_rho", shape=shape,
                initializer=keras.initializers.Constant(self.posterior_rho_init),
                trainable=True,
            ))
        super().build(input_shape)

    def call(self, inputs, training=None):
        kernels = []
        kl = tf.cast(0.0, inputs.dtype)
        for name in ("kernel", "recurrent_kernel", "bias"):
            mu, rho = getattr(self, name + "_mu"), getattr(self, name + "_rho")
            kernels.append(_draw_normal(mu, rho, training))
            kl = kl + _normal_kl(mu, rho, self.prior_sd)
        self.add_loss(kl / tf.cast(self.n_train, kl.dtype))
        kernel, recurrent_kernel, bias = kernels
        n_steps = tf.shape(inputs)[1]
        zeros = tf.zeros((tf.shape(inputs)[0], self.units), dtype=inputs.dtype)

        def step(i, h, c):
            gates = (tf.matmul(inputs[:, i, :], kernel)
                     + tf.matmul(h, recurrent_kernel) + bias)
            gate_i, gate_f, candidate, gate_o = tf.split(gates, 4, axis=-1)
            next_c = tf.sigmoid(gate_f) * c + tf.sigmoid(gate_i) * tf.tanh(candidate)
            next_h = tf.sigmoid(gate_o) * tf.tanh(next_c)
            return i + 1, next_h, next_c

        _, last_h, _ = tf.while_loop(
            lambda i, h, c: i < n_steps,
            step,
            (tf.constant(0), zeros, zeros),
            parallel_iterations=1,
        )
        return last_h

    def compute_output_shape(self, input_shape):
        return (input_shape[0], self.units)

    def get_config(self):
        return {
            **super().get_config(), "units": self.units, "n_train": self.n_train,
            "prior_sd": self.prior_sd,
            "posterior_rho_init": self.posterior_rho_init, "seed": self.seed,
        }


@keras.utils.register_keras_serializable(package="ClimaBayesiano")
class BayesianDense(keras.layers.Layer):
    """Cabeza densa variacional; sus pesos son compartidos por los horizontes."""

    def __init__(self, units, n_train=1, activation=None, prior_sd=1.0,
                 posterior_rho_init=-4.0, seed=42, **kwargs):
        super().__init__(**kwargs)
        self.units = int(units)
        self.n_train = int(n_train)
        self.activation = keras.activations.get(activation)
        self.prior_sd = float(prior_sd)
        self.posterior_rho_init = float(posterior_rho_init)
        self.seed = int(seed)
        if self.units < 1 or self.n_train < 1 or self.prior_sd <= 0:
            raise ValueError("units y n_train deben ser positivos; prior_sd > 0.")

    def build(self, input_shape):
        self.kernel_mu = self.add_weight(
            name="kernel_mu", shape=(int(input_shape[-1]), self.units),
            initializer=keras.initializers.GlorotUniform(seed=self.seed),
            trainable=True,
        )
        self.bias_mu = self.add_weight(
            name="bias_mu", shape=(self.units,), initializer="zeros", trainable=True,
        )
        self.kernel_rho = self.add_weight(
            name="kernel_rho", shape=self.kernel_mu.shape,
            initializer=keras.initializers.Constant(self.posterior_rho_init),
            trainable=True,
        )
        self.bias_rho = self.add_weight(
            name="bias_rho", shape=self.bias_mu.shape,
            initializer=keras.initializers.Constant(self.posterior_rho_init),
            trainable=True,
        )
        super().build(input_shape)

    def call(self, inputs, training=None):
        kernel = _draw_normal(self.kernel_mu, self.kernel_rho, training)
        bias = _draw_normal(self.bias_mu, self.bias_rho, training)
        kl = (_normal_kl(self.kernel_mu, self.kernel_rho, self.prior_sd)
              + _normal_kl(self.bias_mu, self.bias_rho, self.prior_sd))
        self.add_loss(kl / tf.cast(self.n_train, kl.dtype))
        return self.activation(tf.linalg.matmul(inputs, kernel) + bias)

    def get_config(self):
        return {
            **super().get_config(), "units": self.units, "n_train": self.n_train,
            "activation": keras.activations.serialize(self.activation),
            "prior_sd": self.prior_sd,
            "posterior_rho_init": self.posterior_rho_init, "seed": self.seed,
        }


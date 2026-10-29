"""Encoder LSTM variacional y decodificador condicionado al horizonte interno."""
import os
for name in ['TF_NUM_INTRAOP_THREADS', 'TF_NUM_INTEROP_THREADS', 'OMP_NUM_THREADS']:
    os.environ.setdefault(name, '2')
os.environ.setdefault('TF_CPP_MIN_LOG_LEVEL', '2')
import numpy as np
import tensorflow as tf
from tensorflow import keras
from capas_bayesianas import BayesianLSTM, BayesianDense


@keras.utils.register_keras_serializable(package='ClimaV12')
class LeadFeatures(keras.layers.Layer):
    """Posiciones de SALIDA: no son sensores ni columnas adicionales de entrada."""
    def call(self, context):
        h = tf.cast(tf.range(48), context.dtype)
        features = tf.stack([tf.sin(2 * np.pi * h / 24), tf.cos(2 * np.pi * h / 24),
                             h / 47, tf.cast(h >= 24, context.dtype)], axis=-1)
        return tf.tile(features[None], [tf.shape(context)[0], 1, 1])

    def compute_output_shape(self, input_shape):
        return (input_shape[0], 48, 4)


@keras.utils.register_keras_serializable(package='ClimaV12')
def masked_huber(y_true, y_pred):
    n = y_pred.shape[-1]
    target, mask = y_true[..., :n], y_true[..., n:]
    error = tf.where(mask > 0, target - y_pred, 0.)
    absolute = tf.abs(error)
    quadratic = tf.minimum(absolute, 1.)
    value = (.5 * quadratic ** 2 + absolute - quadratic) * mask
    per_feature = tf.reduce_sum(value, axis=1) / tf.maximum(tf.reduce_sum(mask, axis=1), 1.)
    present = tf.cast(tf.reduce_sum(mask, axis=1) > 0, y_pred.dtype)
    return tf.reduce_sum(per_feature * present, axis=-1) / tf.maximum(tf.reduce_sum(present, axis=-1), 1.)


@keras.utils.register_keras_serializable(package='ClimaV12')
def masked_mae(y_true, y_pred):
    n = y_pred.shape[-1]
    target, mask = y_true[..., :n], y_true[..., n:]
    error = tf.where(mask > 0, tf.abs(target - y_pred), 0.)
    return tf.reduce_sum(error * mask, axis=[-1, -2]) / tf.maximum(tf.reduce_sum(mask, axis=[-1, -2]), 1.)


@keras.utils.register_keras_serializable(package='ClimaV12')
class EventLoss(keras.losses.Loss):
    def __init__(self, positive_weight=1., name='event_loss', **kwargs):
        super().__init__(name=name, **kwargs)
        self.positive_weight = float(positive_weight)

    def call(self, y_true, y_pred):
        label, mask = y_true[..., 0], y_true[..., 1]
        value = tf.nn.weighted_cross_entropy_with_logits(label, y_pred[..., 0], self.positive_weight)
        return tf.reduce_sum(value * mask, axis=-1) / tf.maximum(tf.reduce_sum(mask, axis=-1), 1.)

    def get_config(self):
        return {**super().get_config(), 'positive_weight': self.positive_weight}


def build_model(n_features, effective_targets, positive_weight=1., pretrain=False, seed=42):
    keras.utils.set_random_seed(seed)
    history = keras.Input((24, n_features), name='historia_sensores')
    baseline = keras.layers.Concatenate(axis=1, name='patron_24h')([history, history])
    context = BayesianLSTM(8, n_train=effective_targets, posterior_rho_init=-5., seed=seed,
                           name='encoder_bayesiano')(history)
    repeated = keras.layers.RepeatVector(48, name='contexto_por_hora')(context)
    lead = LeadFeatures(name='horizonte_interno')(context)
    decoder = keras.layers.Concatenate(name='contexto_patron_horizonte')([repeated, baseline, lead])
    hidden = BayesianDense(16, n_train=effective_targets, activation='tanh', posterior_rho_init=-5.,
                           seed=seed + 2, name='decoder_bayesiano')(decoder)
    correction = BayesianDense(n_features, n_train=effective_targets, prior_sd=.2, posterior_rho_init=-5.,
                               seed=seed + 3, name='correccion')(hidden)
    numeric = keras.layers.Add(name='variables')([baseline, correction])
    outputs = {'variables': numeric}
    if n_features == 4:
        outputs['lluvia_logit'] = BayesianDense(1, n_train=effective_targets, posterior_rho_init=-5.,
                                               seed=seed + 4, name='lluvia_logit')(hidden)
    model = keras.Model(history, outputs, name=f'LSTM_Bayesiana_V12_{n_features}_Variables')
    head = model.get_layer('correccion')
    head.kernel_mu.assign(tf.zeros_like(head.kernel_mu))
    head.bias_mu.assign(tf.zeros_like(head.bias_mu))
    losses = {'variables': masked_huber}
    weights = {'variables': 1.}
    if n_features == 4:
        losses['lluvia_logit'] = EventLoss(positive_weight)
        weights['lluvia_logit'] = 0. if pretrain else .5
    model.compile(optimizer=keras.optimizers.Adam(.0015, clipnorm=1.), loss=losses,
                  loss_weights=weights, metrics={'variables': [masked_mae]}, jit_compile=False)
    return model


def transfer_encoder(source, destination):
    """Transfiere medias, no una supuesta calibración de lluvia o de lux/Wm²."""
    src, dst = source.get_layer('encoder_bayesiano'), destination.get_layer('encoder_bayesiano')
    rows = int(dst.kernel_mu.shape[0])
    dst.kernel_mu.assign(src.kernel_mu[:rows])
    dst.recurrent_kernel_mu.assign(src.recurrent_kernel_mu)
    dst.bias_mu.assign(src.bias_mu)
    # La dispersión se vuelve a estimar en el dominio local.
    return destination


def transfer_patterns(source, destination):
    """Reutiliza patrones normalizados THL y horizonte; reinicia lluvia local."""
    transfer_encoder(source, destination)
    src, dst = source.get_layer('decoder_bayesiano'), destination.get_layer('decoder_bayesiano')
    n = int(destination.input_shape[-1])
    rows = list(range(8 + n)) + list(range(12, 16))
    dst.kernel_mu.assign(tf.gather(src.kernel_mu, rows, axis=0))
    dst.bias_mu.assign(src.bias_mu)
    src, dst = source.get_layer('correccion'), destination.get_layer('correccion')
    kernel, bias = dst.kernel_mu.numpy(), dst.bias_mu.numpy()
    kernel[:, :3] = src.kernel_mu.numpy()[:, :3]
    bias[:3] = src.bias_mu.numpy()[:3]
    dst.kernel_mu.assign(kernel)
    dst.bias_mu.assign(bias)
    return destination


def mc_predict(model, x, passes=60, seed=42):
    x = np.asarray(x, dtype='float32')
    if x.shape[1:] != tuple(model.input_shape[1:]) or not np.isfinite(x).all():
        raise ValueError('Historia incompleta o incompatible con el modelo')
    keras.utils.set_random_seed(seed)

    @tf.function(reduce_retracing=True)
    def draw(values):
        return model(values, training=True)

    results = {name: [] for name in model.output.keys()}
    for _ in range(passes):
        predicted = draw(x)
        for name in results:
            results[name].append(np.asarray(predicted[name]))
    return {name: np.stack(items) for name, items in results.items()}


def sigmoid(values):
    return 1. / (1. + np.exp(-np.clip(values, -40, 40)))


def physical_outputs(outputs, scaler, positive_weight=1., conditional=True):
    from datos_v12 import inverse
    values = inverse(outputs['variables'], scaler)
    probability = None
    if values.shape[-1] == 4 and 'lluvia_logit' in outputs:
        probability = sigmoid(np.asarray(outputs['lluvia_logit'])[..., 0] - np.log(positive_weight))
        if conditional:
            values[..., 3] *= probability
    return values, probability

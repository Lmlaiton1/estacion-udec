using System.Security.Cryptography;
using System.Text;

namespace RR_Nueva_Naturaleza.Tools
{
    public class Encrypt
    {
        // Factor de costo explícito para no depender del default de la librería: si
        // BCrypt.Net-Next cambiara su default en una versión futura, HashBCrypt y el
        // hash dummy de abajo podrían quedar con costos distintos y romper la garantía
        // de tiempo constante en Auth.
        private const int BCryptWorkFactor = 11;

        // Hash BCrypt precalculado (workFactor 11) de una cadena aleatoria sin relación
        // con ninguna contraseña real, usado únicamente para que la rama "usuario no
        // encontrado" de Auth tarde lo mismo que una verificación real y no permita
        // enumerar cédulas válidas por diferencia de tiempo de respuesta. Es una
        // constante literal (no se calcula en tiempo de ejecución) para que el primer
        // login fallido no pague un costo de inicialización adicional.
        private const string DummyBCryptHash = "$2a$11$rLeKnjFQRfdBFb0mdeIrn.riAywuVtt/VNbGbA2lQs5VScs6pYKye";

        public static string GetSHA256(string str)
        {
            SHA256 sha256 = SHA256Managed.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] stream = null;
            StringBuilder sb = new StringBuilder();
            stream = sha256.ComputeHash(encoding.GetBytes(str));
            for (int i = 0; i < stream.Length; i++) sb.AppendFormat("{0:x2}", stream[i]);
            return sb.ToString();
        }

        // Un hash SHA-256 en hexadecimal siempre tiene 64 caracteres; un hash BCrypt
        // (formato $2a$/$2b$...) nunca tiene esa longitud exacta. Suficiente para
        // distinguir un hash legado de uno ya migrado sin guardar un flag aparte.
        public static bool IsLegacySha256Hash(string hash)
        {
            return !string.IsNullOrEmpty(hash) && hash.Length == 64;
        }

        public static string HashBCrypt(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: BCryptWorkFactor);
        }

        public static bool VerifyBCrypt(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public static void VerifyDummyBCrypt(string password)
        {
            BCrypt.Net.BCrypt.Verify(password, DummyBCryptHash);
        }
    }
}

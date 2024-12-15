// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("Gdkm2oJC6V4ssevGS3F0PYNUVxzZvtFPOUglqImMveg0xU6UcdwGg843ZSoVo8I0VvmUsiktkJxYlFRcbO/h7t5s7+TsbO/v7lt8tKti3CEuT89ICWKAke7JNHPUzkxD7qIJD1X4HoCuPopiBDduFM43SunzglOlqjjVF+0qMnLubvDkgoeaEqPGlxvebO/M3uPo58RopmgZ4+/v7+vu7csZAfh0TF4ZzQCzDEDhIsyEBaohxGr/PDSjLLjQqoAr9/IM0YomrDynMqfEkHrJpXWcb/mMxRbU3GBssKT6mLH7UCL7hqHv7+WGtATI3STeqXE2yDLA4jbvWdG5hXUePlmcU32HPK7uNi3D2L9qXdebPdYi+ZaolopVXWxw/+GYfezt7+7v");
        private static int[] order = new int[] { 8,12,9,12,4,9,9,8,9,11,10,11,12,13,14 };
        private static int key = 238;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}

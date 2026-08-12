namespace TERMS_LOYALTY_API.DTOs.shelf
{
    public class MinewLogin
    {
        public class MinewLoginRequest
        {
            public string username { get; set; }
            public string password { get; set; }
        }
        public class MinewLoginResponse
        {
            public int code { get; set; }
            public string msg { get; set; }
            public MinewLoginData data { get; set; }
        }
        public class MinewLoginData
        {
            public string token { get; set; }
        }
    }
}

using ChatApp.Database;
using ChatApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Utilities
{
    public static class Configuration
    {
        public static readonly string BackendURL = "https://your-backend.azurewebsites.net/";
        public static readonly string FrontendNoHTTP = "your-frontend.azurewebsites.net";
        public static readonly string FrontendURL = "https://your-frontend.azurewebsites.net/";
        public static readonly string FrontendURLNoSlash = "https://your-frontend.azurewebsites.net";

        /// <summary>
        /// You also need to update frontend url in CharsPopup.xaml manuelly 
        /// </summary>
    }
}

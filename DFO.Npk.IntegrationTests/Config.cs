using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.Npk.IntegrationTests
{
    class Config
    {
        public string ImageNpkDir { get; private set; }
        
        public Config()
        {
            ImageNpkDir = ConfigurationManager.AppSettings["ImageNpkDir"];
            if (ImageNpkDir == null)
            {
                throw new Exception("ImageNpkDir appsetting not set.");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace yamlConfig
{
    public class Config
    {
        public string Port { get; set; }
        public int Baudrate { get; set; }
        public List<object> Apps { get; set; }
    }
}

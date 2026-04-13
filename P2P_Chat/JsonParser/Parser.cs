using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;


namespace P2P_Chat.JsonParser
{
    
    class Parser
    {

        public Parser()
        {

        }

        public static string ParseModelToJson(Model DataModel)
        {
            return JsonSerializer.Serialize(DataModel);
        }

        public static Model? ParseJsonToModel(string json)
        {
            return JsonSerializer.Deserialize<Model>(json);
        }

    }
}

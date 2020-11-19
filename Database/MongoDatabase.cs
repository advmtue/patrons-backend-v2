using System;
namespace patrons_web_api.Database
{
    public class MongoDatabase : IPatronsDatabase
    {
        public MongoDatabase()
        {
            Console.WriteLine("Instantiated new mongodatabase");
        }

        public string getHelloWorld()
        {
            return "Hello world";
        }
    }
}
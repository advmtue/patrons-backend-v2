using System;

using patrons_web_api.Database;

namespace patrons_web_api.Services
{
    public class ManagerService
    {
        private IPatronsDatabase _database;

        public ManagerService(IPatronsDatabase db)
        {
            Console.WriteLine("Instantiated new managerservice");

            // Save refs
            _database = db;
        }

        public string getHelloWorldFromDatabase()
        {
            return _database.getHelloWorld();
        }
    }
}
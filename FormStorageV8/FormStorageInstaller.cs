using FormStorage.Models;

using System.Xml.Linq;

using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.PackageActions;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;

namespace FormStorage.Installer
{
    public class MigrationCreateTables : MigrationBase
    {
        public MigrationCreateTables(IMigrationContext context)
            : base(context)
        { }

        public override void Migrate()
        {
            if (!TableExists("FormStorageForms"))
            {
                Create.Table<FormStorageFormModel>().Do();
                Logger.Info<MigrationCreateTables>("Created FormStorageForms table.");
            }
            if (!TableExists("FormStorageSubmissions"))
            {
                Create.Table<FormStorageSubmissionModel>().Do();
                Logger.Info<MigrationCreateTables>("Created FormStorageSubmissions table.");
            }
            if (!TableExists("FormStorageEntries"))
            {
                Create.Table<FormStorageEntryModel>().Do();
                Logger.Info<MigrationCreateTables>("Created FormStorageEntries table.");
            }
        }
    }

    public class FormStorageMigrationPlan : MigrationPlan
    {
        public FormStorageMigrationPlan() : base("FormStorageV8")
        {
            From(string.Empty).To<MigrationCreateTables>("first-migration");
        }
    }

    public class ConfigureDatabase : IPackageAction
    {
        private readonly IScopeProvider scopeProvider;
        private readonly IMigrationBuilder migrationBuilder;
        private readonly IKeyValueService keyValueService;
        private readonly ILogger logger;

        public ConfigureDatabase(IScopeProvider scopeProvider, IMigrationBuilder migrationBuilder,
                                 IKeyValueService keyValueService, ILogger logger)
        {
            this.scopeProvider = scopeProvider;
            this.migrationBuilder = migrationBuilder;
            this.keyValueService = keyValueService;
            this.logger = logger;
        }

        public string Alias()
        {
            return "FormStorage_ConfigureDatabase";
        }

        public bool Execute(string packageName, XElement xmlData)
        {
            var upgrader = new Upgrader(new FormStorageMigrationPlan());
            upgrader.Execute(scopeProvider, migrationBuilder, keyValueService, logger);
            return true;
        }

        public bool Undo(string packageName, XElement xmlData)
        {
            return true;
        }

    }
}
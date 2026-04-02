using NbnStock.Core.Data;
using NbnStock.Core.Models;
using NbnStock.Core.Repositories;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DatabaseInitialiser.Initialise();

            /* var repo = new SerialisedUnitRepository();

            int newId = repo.AddSerialisedUnit(new SerialisedUnit
            {
                StockItemId = 1,
                SerialNumber = "TESTSERIAL006",
                Status = UnitStatus.OnHand.ToString(),
                Notes = "Test serialised unit",
                LastUpdatedUtc = DateTime.UtcNow
            });

            repo.UpdateSerialisedUnitStatus(newId, UnitStatus.EwastePendingSubmission.ToString()); */
        }
    }
}
using Microsoft.EntityFrameworkCore;
using TreningsAppHaffi.Data;

namespace TreningsAppHaffi.Tests
{
    public class SQLtestModelTests
    {
        [Fact]
        public async Task OnPostInsert_SetsHiddenToFalse()
        {
            /*
             * Gjort som en lengre oppgave med detaljerte beskrivelser av ChatGPT.
             * Første oppgaven gjaldt å ha 'arrange, act, assert' tydelig i tankene når man skriver tester.
             * Og denne metoden ble spesifikt valgt for å demonstrere hvordan man 'takler' en 'fake' in-memory-database input i en test.
             * 
             * Microsoft.EntityFrameworkCore.InMemory installert igjenom NuGet for å kunne bruke InMemoryDatabase for testing.
             * <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.3" />
             * Fordi AzureSql serveren bruker den eldre 10.0.3 versionen og ikke 10.0.11 som er nyeste nytt. Og dette skal matche.
             */

            // Arrange
            var options = new DbContextOptionsBuilder<MyDatabaseContext>()
                .UseInMemoryDatabase("SQLtestTestDatabase")
                .Options;

            // Create SQLtestModel
            // Give it some input

            // Act
            //await model.OnPostInsert();

            // Assert
            //entry.Hidden.Should().BeFalse();
        }
    }
}

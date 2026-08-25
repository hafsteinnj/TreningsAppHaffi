using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TreningsAppHaffi.Data;
using TreningsAppHaffi.Pages;

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

            var context = new MyDatabaseContext(options);
            var model = new SQLtestModel(context);

            model.JobId = 1;
            model.Description = "Unit test";
            model.Text = "Testing OnPostInsert";
            model.Minutes = 30;

            // Act
            await model.OnPostInsertAsync();
            var entry = context.TestEntries.Single();

            // Assert
            entry.Hidden.Should().BeFalse();
        }

        [Fact]
        public async Task OnGetEntries_ReturnsEntriesInDescendingDateOrder()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MyDatabaseContext>()
                .UseInMemoryDatabase("SQLtestOrderingTestDatabase")
                .Options;

            var context = new MyDatabaseContext(options);
            var model = new SQLtestModel(context);

            /*
             * Adding 3 dates out of order
             * Oldest   → 10:00
             * Newest   → 12:00
             * Middle   → 11:00
             */

            context.TestEntries.AddRange(
                new TestEntry
                {
                    UserId = 0,
                    JobId = 0,
                    CreatedDate = new DateTime(2026, 8, 20, 10, 0, 0),
                    Description = "Oldest",
                    Text = "Test",
                    Minutes = 0,
                    Hidden = false
                },
                new TestEntry
                {
                    UserId = 0,
                    JobId = 0,
                    CreatedDate = new DateTime(2026, 8, 20, 12, 0, 0),
                    Description = "Newest",
                    Text = "Test",
                    Minutes = 0,
                    Hidden = false
                },
                new TestEntry
                {
                    UserId = 0,
                    JobId = 0,
                    CreatedDate = new DateTime(2026, 8, 20, 11, 0, 0),
                    Description = "Middle",
                    Text = "Test",
                    Minutes = 0,
                    Hidden = false
                }
            );

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Act
            var result = await model.OnGetEntriesAsync();

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var entries = jsonResult.Value.Should().BeAssignableTo<List<TestEntry>>().Subject;

            entries.Select(e => e.Description)
                .Should()
                .ContainInOrder("Newest", "Middle", "Oldest");
            /*
             * I expect the output to be newest first, then middle, and latest,
             * I do not foresee this webpage in particular changing from this layout.
             * As such i think it's a decent enough thing to test...
             */
        }

        // Made using Claude
        // Complements OnPostInsert_SetsHiddenToFalse above, which only checks the
        // Hidden flag. This one guards against a swapped/dropped field mapping bug
        // (e.g. Description and Text accidentally swapped) that the Hidden-only
        // assertion would never catch.
        [Fact]
        public async Task OnPostInsert_MapsAllBoundPropertiesCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MyDatabaseContext>()
                .UseInMemoryDatabase("SQLtestMappingTestDatabase")
                .Options;

            var context = new MyDatabaseContext(options);
            var model = new SQLtestModel(context);

            model.JobId = 42;
            model.Description = "Mapping test description";
            model.Text = "Mapping test text";
            model.Minutes = 15;

            // Act
            await model.OnPostInsertAsync();
            var entry = context.TestEntries.Single();

            // Assert
            entry.JobId.Should().Be(42);
            entry.Description.Should().Be("Mapping test description");
            entry.Text.Should().Be("Mapping test text");
            entry.Minutes.Should().Be(15);
        }

        // Made using Claude
        // Companion to the ordering test above - checks the "nothing in the
        // database yet" path returns an empty list rather than null or an error.
        [Fact]
        public async Task OnGetEntries_WhenDatabaseEmpty_ReturnsEmptyList()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MyDatabaseContext>()
                .UseInMemoryDatabase("SQLtestEmptyTestDatabase")
                .Options;

            var context = new MyDatabaseContext(options);
            var model = new SQLtestModel(context);

            // Act
            var result = await model.OnGetEntriesAsync();

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var entries = jsonResult.Value.Should().BeAssignableTo<List<TestEntry>>().Subject;

            entries.Should().BeEmpty();
        }

        // Made using Claude
        // Happy-path only. With UseInMemoryDatabase, CanConnectAsync() will
        // essentially always return true, so the "server unreachable" catch
        // branch in OnGetCheckConnectionAsync isn't realistically reachable
        // at this level - that would need an actual (or mocked) SQL provider.
        [Fact]
        public async Task OnGetCheckConnection_WhenDatabaseAvailable_ReturnsConnectedTrue()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MyDatabaseContext>()
                .UseInMemoryDatabase("SQLtestConnectionTestDatabase")
                .Options;

            var context = new MyDatabaseContext(options);
            var model = new SQLtestModel(context);

            // Act
            var result = await model.OnGetCheckConnectionAsync();

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().BeEquivalentTo(new
            {
                connected = true,
                message = "Connected to SQL server."
            });
        }
    }
}

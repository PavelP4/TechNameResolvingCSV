using ConsoleApp1.CsvMaps;
using ConsoleApp1.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace ConsoleApp1
{
    internal class Program
    {
        const string DbConnectionString = "Data Source=SC0895\\SQLEXPRESS; Initial Catalog=CVPRelease; User Id=sa; Password=12345678; Integrated Security=True; MultipleActiveResultSets=true;Connect Timeout=60;Encrypt=False;TrustServerCertificate=false";

        const string ResulvedListOfTermsPath = "Data\\KeyValuePairsByUniqueOnly.json";
        const string ImportTechnologiesPath = "Output\\ImportTechnologies.csv";
        const string MatchingTechnologiesPath = "Output\\technologies.csv";

        static async Task Main(string[] args)
        {
            var initialItems = await LoadInitialMap();
            Console.WriteLine($"InitialListOfTerms: {initialItems.Count()}");

            var resolvedItems = LoadResolvedListOfTerms();
            Console.WriteLine($"ResolvedListOfTerms: {resolvedItems.Count()}");

            var unresolvedItems = GetUnresolvedItems(initialItems, resolvedItems);
            if (unresolvedItems.Any())
            {
                throw new Exception("Unresolved items found.");
            }

            var technologyMapItems = new List<TechnologyMapItem>();
            foreach (var initialItem in initialItems)
            {
                var originalKey = ToTechnologyKey(initialItem.TechnologyName);

                if (string.IsNullOrWhiteSpace(originalKey))
                {
                    continue;
                }

                var resolvedTechnologyNames = resolvedItems.FirstOrDefault(x => x.Key.Equals(originalKey))?.Value ?? [];

                foreach (var resolvedTechnologyName in resolvedTechnologyNames)
                {
                    technologyMapItems.Add(new TechnologyMapItem()
                    {
                        TechnologyGroup = initialItem.TechnologyGroup.Trim(),
                        TechnologyName = resolvedTechnologyName.Trim()
                    });
                }
            }

            technologyMapItems = technologyMapItems.Distinct().ToList();
            BuildImportTechnologiesCsvFile(technologyMapItems);


            var technologyMatchingMapItems = new List<MatchingMapModel>();
            var oldTechnologyNames = initialItems.Select(x => x.TechnologyName).Distinct().ToList();
            foreach (var oldTechnologyName in oldTechnologyNames)
            {
                var originalKey = ToTechnologyKey(oldTechnologyName);

                if (string.IsNullOrWhiteSpace(originalKey))
                {
                    continue;
                }

                var resolvedTechnologyNames = resolvedItems.FirstOrDefault(x => x.Key.Equals(originalKey))?.Value ?? [];

                foreach (var resolvedTechnologyName in resolvedTechnologyNames)
                {
                    technologyMatchingMapItems.Add(new MatchingMapModel()
                    {
                        OldTechnology = oldTechnologyName,
                        NewTechnology = resolvedTechnologyName.Trim(),
                    });
                }
            }
            BuildMatchingTechnologiesCsvFile(technologyMatchingMapItems);



            Console.WriteLine("Done!");
        }

        static string ToTechnologyKey(string initial)
        { 
            return initial.Replace("\r\n", " ").Trim().ToLower();
        }

        static async Task<IEnumerable<TechnologyMapItem>> LoadInitialMap()
        {
            using var dbConnection = CreateDbConnection();

            var query = """
                SELECT distinct e.[Group] TechnologyGroup, t.[TechnologyName] TechnologyName
                  FROM [CVPRelease].[dbo].[Technologies_OLD] t
                	inner join [CVPRelease].[dbo].[Expertises] e on t.ExpertiseId = e.ExpertiseId
                order by 1, 2
                """;

            var result = await dbConnection.QueryAsync<TechnologyMapItem>(query);

            return result;
        }

        static IDbConnection CreateDbConnection()
        {
            return new SqlConnection(DbConnectionString);
        }

        static IEnumerable<TechnologyKeyValuesItem> LoadResolvedListOfTerms()
        { 
            var fileText = File.ReadAllText(ResulvedListOfTermsPath);

            fileText = fileText.Replace(@"\", @"\\");

            var result = (JsonSerializer.Deserialize<Dictionary<string, string[]>>(fileText) ?? [])
                .Select(x => new TechnologyKeyValuesItem()
                { 
                    Key  = x.Key.Trim().ToLower(),
                    Value = x.Value,
                })
                .ToArray();

            return result;
        }

        static string[] GetUnresolvedItems(IEnumerable<TechnologyMapItem> initial, IEnumerable<TechnologyKeyValuesItem> resolved)
        {
            var initialTechnolyNames = initial.Select(x => ToTechnologyKey(x.TechnologyName)).Distinct().ToArray();
            var resolvedTechnolyNames = resolved.Select(x => x.Key).Distinct().ToArray();

            return (
                from initialItem in initialTechnolyNames
                join resolvedItem in resolvedTechnolyNames on initialItem equals resolvedItem into joinedItem
                    from item in joinedItem.DefaultIfEmpty()
                where item == null
                select initialItem
            ).ToArray();
        }

        static IEnumerable<TechnologyKeyValuesItem> FilterResolvedListOfTerms(string[] initial, IEnumerable<TechnologyKeyValuesItem> resolved)
        {
            return initial.Join(resolved, o => o, i => i.Key, (o, i) => i).ToArray();
        }

        static void BuildImportTechnologiesCsvFile(IEnumerable<TechnologyMapItem> items, string delimiter = ";")
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter
            };

            CreateDirIfNotExists(ImportTechnologiesPath);

            using var writer = new StreamWriter(ImportTechnologiesPath);
            using var csv = new CsvWriter(writer, config);
            
            csv.Context.RegisterClassMap<TechnologyAndTechnologyGroupMap>();
            
            csv.WriteRecords(items);
        }

        static void BuildMatchingTechnologiesCsvFile(IEnumerable<MatchingMapModel> items, string delimiter = ";")
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter
            };

            CreateDirIfNotExists(MatchingTechnologiesPath);

            using var writer = new StreamWriter(MatchingTechnologiesPath);
            using var csv = new CsvWriter(writer, config);

            csv.Context.RegisterClassMap<TechnologyMatchingMap>();

            csv.WriteRecords(items);
        }

        static void CreateDirIfNotExists(string filePath)
        { 
            var dir = new FileInfo(filePath).Directory?.FullName;

            if (dir == null) 
            {
                return;
            }

            if (!Directory.Exists(dir))
            { 
                Directory.CreateDirectory(dir);
            }
        }
    }
}

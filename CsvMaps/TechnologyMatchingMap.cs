using ConsoleApp1.Models;
using CsvHelper.Configuration;

namespace ConsoleApp1.CsvMaps
{
    public class TechnologyMatchingMap : ClassMap<MatchingMapModel>
    {
        public TechnologyMatchingMap()
        {
            Map(m => m.OldTechnology).Name("Current Tech");
            Map(m => m.NewTechnology).Name("New Tech");
        }
    }
}
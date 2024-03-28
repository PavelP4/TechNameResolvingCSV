using ConsoleApp1.Models;
using CsvHelper.Configuration;

namespace ConsoleApp1.CsvMaps
{
    internal sealed class TechnologyAndTechnologyGroupMap : ClassMap<TechnologyMapItem>
    {
        public TechnologyAndTechnologyGroupMap()
        {
            Map(m => m.TechnologyGroup).Name("TechnologyGroup");
            Map(m => m.TechnologyName).Name("Technology");
        }
    }
}

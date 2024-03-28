using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Models
{
    internal class TechnologyMapItem : IEquatable<TechnologyMapItem>
    {
        public string TechnologyGroup { get; set; } = string.Empty;
        public string TechnologyName { get; set; } = string.Empty;

        public bool Equals(TechnologyMapItem? other)
        {
            if (other is null) return false;

            if (ReferenceEquals(this, other)) return true;

            return TechnologyGroup.Equals(other.TechnologyGroup, StringComparison.CurrentCultureIgnoreCase)
                && TechnologyName.Equals(other.TechnologyName, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TechnologyMapItem);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TechnologyGroup, TechnologyName);
        }
    }
}

using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace NadekoBot.Services.Database.Models
{
    public class ConvertUnit : DbEntity
    {
        public ConvertUnit() { }
        [NotMapped]
        private string[] _triggersValue;
        [NotMapped]
        public string[] Triggers
        {
            get
            {
                return _triggersValue ?? (_triggersValue = InternalTrigger.Split('|'));
            }
            set
            {
                _triggersValue = value;
                InternalTrigger = string.Join("|", _triggersValue);
            }
        }
        //protected or private?
        /// <summary>
        /// DO NOT CALL THIS
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string InternalTrigger { get; set; }
        public string UnitType { get; set; }
        public decimal Modifier { get; set; }

        public override bool Equals(object obj)
        {
            var cu = obj as ConvertUnit;
            if (cu == null)
                return false;
            return cu.UnitType == this.UnitType;
        }

        public override int GetHashCode()
        {
            return this.UnitType.GetHashCode();
        }
    }

}

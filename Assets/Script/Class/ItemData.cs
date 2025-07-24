using System.Collections.Generic;
using OptionDataNamespace;

namespace ItemDataNamespace
{
    public class ItemData
    {
        public int itemId, color, type, img, quantity, upgrade;
        public string name;
        public List<OptionData> options;
    }
}

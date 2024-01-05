using System.Collections.ObjectModel;

namespace NetworkToolBox {
    public class Container {
        public Container() {
            Childs = new ObservableCollection<object>();
        }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }
        public ObservableCollection<object> Childs { get; set; }
        public override string ToString() {
            return Name;
        }
    }

}

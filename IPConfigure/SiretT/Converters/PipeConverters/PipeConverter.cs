using System.Windows.Data;

namespace SiretT.Converters {
    public interface IPipe {
        IPipeConverter PipeConverter { get; set; }
    }

    public interface IPipeConverter : IValueConverter, IPipe { }
}

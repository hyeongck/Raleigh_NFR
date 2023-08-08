namespace ClothoSharedItems
{
    public interface IPowerModule
    {
        string Name { get; }
        string FullName { get; }
        DevBase Owner { get; }
    }

    public interface IPowerModuleEnumerable
    {
        T[] GetPowerModules<T>() where T : IPowerModule;
    }

    public interface IVoltMeasure : IPowerModule
    {
        double MeasureVolt();
    }

    public interface ICurrMeasure : IPowerModule
    {
        double MeasureCurr();
    }

    public interface IPowerSupply : IVoltMeasure, ICurrMeasure, IPowerModule
    {
        bool OutputOn { get; set; }
        double VoltTarget { get; set; }
        double CurrLimit { get; set; }
    }
}
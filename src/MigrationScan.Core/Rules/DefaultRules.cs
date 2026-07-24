using MigrationScan.Core.Engine;

namespace MigrationScan.Core.Rules;

/// <summary>
/// Builds the built-in rule set, wiring each rule to its metadata from the catalog.
/// This is the single place that lists which rules ship.
/// </summary>
public static class DefaultRules
{
    public static IReadOnlyList<IMigrationRule> CreateAll(
        RuleCatalog catalog,
        PackageCompatibilityCatalog packageCatalog) =>
    [
        new Mig1001NonSdkProject(catalog.Get(Mig1001NonSdkProject.Id)),
        new Mig1002PackagesConfig(catalog.Get(Mig1002PackagesConfig.Id)),
        new Mig1005GacReference(catalog.Get(Mig1005GacReference.Id)),
        new Mig2001IncompatiblePackage(catalog.Get(Mig2001IncompatiblePackage.Id), packageCatalog),
        new Mig2002DeprecatedPackage(catalog.Get(Mig2002DeprecatedPackage.Id)),
        new Mig3001WebForms(catalog.Get(Mig3001WebForms.Id)),
        new Mig3002SystemWeb(catalog.Get(Mig3002SystemWeb.Id)),
        new Mig3004WcfServiceHost(catalog.Get(Mig3004WcfServiceHost.Id)),
        new Mig3005Remoting(catalog.Get(Mig3005Remoting.Id)),
        new Mig3010AspNetMvc5(catalog.Get(Mig3010AspNetMvc5.Id)),
        new Mig4001SystemDrawingCommon(catalog.Get(Mig4001SystemDrawingCommon.Id)),
        new Mig4002Registry(catalog.Get(Mig4002Registry.Id)),
        new Mig4004DirectoryServices(catalog.Get(Mig4004DirectoryServices.Id)),
        new Mig4008ThreadAbort(catalog.Get(Mig4008ThreadAbort.Id)),
        new Mig5001ConfigurationManagerAppSettings(catalog.Get(Mig5001ConfigurationManagerAppSettings.Id)),
        new Mig6001BinaryFormatter(catalog.Get(Mig6001BinaryFormatter.Id)),
        new Mig6004CodeAccessSecurity(catalog.Get(Mig6004CodeAccessSecurity.Id)),
        new Mig7001SystemDataSqlClient(catalog.Get(Mig7001SystemDataSqlClient.Id)),
        new Mig8002EncodingDefault(catalog.Get(Mig8002EncodingDefault.Id)),
        new Mig8003CodePageEncoding(catalog.Get(Mig8003CodePageEncoding.Id)),
        new Mig1003OldTargetFramework(catalog.Get(Mig1003OldTargetFramework.Id)),
        new Mig3003Asmx(catalog.Get(Mig3003Asmx.Id)),
        new Mig3009Msmq(catalog.Get(Mig3009Msmq.Id)),
        new Mig4003Wmi(catalog.Get(Mig4003Wmi.Id)),
        new Mig4005EventLog(catalog.Get(Mig4005EventLog.Id)),
        new Mig6005ObsoleteCrypto(catalog.Get(Mig6005ObsoleteCrypto.Id)),
        new Mig7003OleDb(catalog.Get(Mig7003OleDb.Id)),
        new Mig7006LinqToSql(catalog.Get(Mig7006LinqToSql.Id)),
        new Mig3015WcfClient(catalog.Get(Mig3015WcfClient.Id)),
    ];
}

using System.Reflection;

public class VersionProvider {
  public static string? GetVersion() {
    string? implementationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    return implementationVersion;
  }

  public static string GetVersionComplete() {
    return $"eark-sip-4net version {GetVersion() ?? "unknown"}";
  }
}
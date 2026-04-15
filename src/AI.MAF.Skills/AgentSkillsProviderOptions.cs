using Microsoft.Agents.AI;

namespace AI.MAF.Skills;

/// <summary>
/// Options for configuring the <see cref="AgentSkillsProvider"/>.
/// </summary>
#pragma warning disable MAAI001 // Agent skills types are marked experimental
public sealed class AgentSkillsProviderOptions
{
    /// <summary>
    /// Gets or sets the path to file-based skills directory.
    /// When set to a relative path, it is resolved relative to <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public string? FileSkillsPath { get; set; }

    /// <summary>
    /// Gets or sets whether script execution requires user approval.
    /// Default is <c>false</c>.
    /// </summary>
    public bool RequireApproval { get; set; }

    /// <summary>
    /// Gets or sets the script runner for file-based skills.
    /// Required when <see cref="FileSkillsPath"/> is specified.
    /// Use <c>SubprocessScriptRunner.RunAsync</c> for production or provide a custom implementation.
    /// </summary>
    public AgentFileSkillScriptRunner? FileScriptRunner { get; set; }
}
#pragma warning restore MAAI001

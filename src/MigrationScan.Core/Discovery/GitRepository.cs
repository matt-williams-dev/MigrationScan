namespace MigrationScan.Core.Discovery;

/// <summary>
/// Reads the commit a scanned directory is sitting on, by reading <c>.git</c> directly.
/// </summary>
/// <remarks>
/// Deliberately does not shell out to <c>git</c>: the binary may not be installed on a locked-down
/// build server, and spawning a process from an offline analysis tool is a surprise nobody asked
/// for. Everything here is a plain file read, and every failure path is "no commit" — provenance
/// is a convenience, and never a reason for a scan to fail.
/// </remarks>
public static class GitRepository
{
    /// <summary>
    /// The commit SHA <paramref name="directory"/> is checked out at, or null when it is not in a
    /// git working tree (or the repository is in a state this reader does not understand).
    /// </summary>
    public static string? CommitOf(string directory)
    {
        try
        {
            return FindGitDirectory(directory) is { } gitDirectory ? ReadHead(gitDirectory) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Walks up looking for <c>.git</c>. It is normally a directory, but a worktree or submodule
    /// checkout leaves a file holding a <c>gitdir:</c> pointer instead.
    /// </summary>
    private static string? FindGitDirectory(string directory)
    {
        for (DirectoryInfo? current = new(directory); current is not null; current = current.Parent)
        {
            string candidate = Path.Combine(current.FullName, ".git");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(candidate))
            {
                string content = File.ReadAllText(candidate).Trim();
                const string prefix = "gitdir:";
                if (content.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string pointer = content[prefix.Length..].Trim();
                    return Path.IsPathRooted(pointer) ? pointer : Path.Combine(current.FullName, pointer);
                }
            }
        }

        return null;
    }

    private static string? ReadHead(string gitDirectory)
    {
        string headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        string head = File.ReadAllText(headPath).Trim();

        // Detached HEAD: the file holds the SHA itself.
        const string refPrefix = "ref:";
        if (!head.StartsWith(refPrefix, StringComparison.Ordinal))
        {
            return IsSha(head) ? head : null;
        }

        string reference = head[refPrefix.Length..].Trim();

        // A loose ref is a file under .git; a ref that has been packed lives in packed-refs.
        string loose = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(loose))
        {
            string sha = File.ReadAllText(loose).Trim();
            return IsSha(sha) ? sha : null;
        }

        return ReadPackedRef(gitDirectory, reference);
    }

    private static string? ReadPackedRef(string gitDirectory, string reference)
    {
        string packed = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packed))
        {
            return null;
        }

        foreach (string line in File.ReadLines(packed))
        {
            // Format: "<sha> <refname>". Comments start with '#', peeled tags with '^'.
            if (line.Length == 0 || line[0] is '#' or '^')
            {
                continue;
            }

            int space = line.IndexOf(' ');
            if (space > 0 && line[(space + 1)..].Trim() == reference)
            {
                string sha = line[..space];
                return IsSha(sha) ? sha : null;
            }
        }

        return null;
    }

    private static bool IsSha(string value) =>
        value.Length is 40 or 64 && value.All(char.IsAsciiHexDigit);
}

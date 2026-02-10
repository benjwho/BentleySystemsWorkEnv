using System.Text;
using System.Text.Json;
using INWC.Automation.Cli.Domain.Runtime;

namespace INWC.Automation.Cli.Infrastructure.Runtime;

internal sealed class RuntimeQueueStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly RuntimePathProvider _pathProvider;

    public RuntimeQueueStore(RuntimePathProvider pathProvider)
    {
        _pathProvider = pathProvider;
        _pathProvider.EnsureRuntimeDirectories();
    }

    public void Enqueue(RuntimeActionIntent intent, RuntimeQueueState state)
    {
        var root = GetRoot(state);
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, intent.Id + ".json");
        WriteJsonAtomic(path, intent);
    }

    public bool TryApprove(string actionId, string? note, out RuntimeActionIntent? intent)
    {
        return TryMove(actionId, _pathProvider.QueuePendingRoot, _pathProvider.QueueApprovedRoot, note, out intent);
    }

    public bool TryReject(string actionId, string? note, out RuntimeActionIntent? intent)
    {
        return TryMove(actionId, _pathProvider.QueuePendingRoot, _pathProvider.QueueRejectedRoot, note, out intent);
    }

    public bool TryDequeueApproved(out RuntimeQueueEntry? entry)
    {
        entry = null;
        var file = GetOldestFile(_pathProvider.QueueApprovedRoot);
        if (file is null)
        {
            return false;
        }

        var target = Path.Combine(_pathProvider.QueueInProgressRoot, Path.GetFileName(file));
        try
        {
            Directory.CreateDirectory(_pathProvider.QueueInProgressRoot);
            File.Move(file, target);
            var intent = ReadJson<RuntimeActionIntent>(target);
            entry = new RuntimeQueueEntry(RuntimeQueueState.InProgress, target, intent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Complete(RuntimeQueueEntry inProgressEntry, RuntimeRunRecord runRecord)
    {
        var runFolder = Path.Combine(_pathProvider.RunsRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(runFolder);
        var runPath = Path.Combine(runFolder, $"{runRecord.IntentId}.json");
        WriteJsonAtomic(runPath, runRecord);

        if (File.Exists(inProgressEntry.Path))
        {
            File.Delete(inProgressEntry.Path);
        }
    }

    public IReadOnlyList<RuntimeQueueEntry> ListAll()
    {
        var list = new List<RuntimeQueueEntry>();
        list.AddRange(ReadEntries(RuntimeQueueState.Pending, _pathProvider.QueuePendingRoot));
        list.AddRange(ReadEntries(RuntimeQueueState.Approved, _pathProvider.QueueApprovedRoot));
        list.AddRange(ReadEntries(RuntimeQueueState.InProgress, _pathProvider.QueueInProgressRoot));
        list.AddRange(ReadEntries(RuntimeQueueState.Rejected, _pathProvider.QueueRejectedRoot));
        return list
            .OrderBy(e => e.Intent.CreatedUtc)
            .ToList();
    }

    private bool TryMove(string actionId, string fromRoot, string toRoot, string? note, out RuntimeActionIntent? intent)
    {
        intent = null;
        var source = Path.Combine(fromRoot, actionId + ".json");
        if (!File.Exists(source))
        {
            return false;
        }

        try
        {
            var loaded = ReadJson<RuntimeActionIntent>(source);
            loaded.Note = note;
            var target = Path.Combine(toRoot, Path.GetFileName(source));
            Directory.CreateDirectory(toRoot);
            WriteJsonAtomic(target, loaded);
            File.Delete(source);
            intent = loaded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private IReadOnlyList<RuntimeQueueEntry> ReadEntries(RuntimeQueueState state, string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var list = new List<RuntimeQueueEntry>();
        foreach (var path in Directory.EnumerateFiles(root, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var intent = ReadJson<RuntimeActionIntent>(path);
                list.Add(new RuntimeQueueEntry(state, path, intent));
            }
            catch
            {
                // ignored on purpose
            }
        }

        return list;
    }

    private static string? GetOldestFile(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        return Directory.EnumerateFiles(root, "*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderBy(info => info.CreationTimeUtc)
            .Select(info => info.FullName)
            .FirstOrDefault();
    }

    private string GetRoot(RuntimeQueueState state)
    {
        return state switch
        {
            RuntimeQueueState.Pending => _pathProvider.QueuePendingRoot,
            RuntimeQueueState.Approved => _pathProvider.QueueApprovedRoot,
            RuntimeQueueState.InProgress => _pathProvider.QueueInProgressRoot,
            RuntimeQueueState.Rejected => _pathProvider.QueueRejectedRoot,
            _ => _pathProvider.QueuePendingRoot
        };
    }

    private static T ReadJson<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json)
               ?? throw new InvalidOperationException("Failed to deserialize JSON: " + path);
    }

    private static void WriteJsonAtomic<T>(string path, T model)
    {
        var dir = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid path: " + path);
        Directory.CreateDirectory(dir);

        var temp = path + ".tmp";
        var json = JsonSerializer.Serialize(model, JsonOptions);
        File.WriteAllText(temp, json, Encoding.ASCII);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(temp, path);
    }
}

using System.Data;

namespace thaibev.Data;

// Value metadata only. Each table service owns its actual column definitions.
public sealed record SqlColumn(string Name, SqlDbType Type, int Length = 0, bool Nullable = false, byte Precision = 0, byte Scale = 0);

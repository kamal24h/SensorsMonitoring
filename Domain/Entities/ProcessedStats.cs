using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class ProcessedStats
{
    public int Id { get; private set; }
    public int TotalLines { get; private set; }
    public int ReadingsStored { get; private set; }
    public int DuplicatesRemoved { get; private set; }
    public int InvalidRecords { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProcessedStats() { }

    public ProcessedStats(int totalLines, int readingsStored, int duplicatesRemoved, int invalidRecords)
    {
        TotalLines = totalLines;
        ReadingsStored = readingsStored;
        DuplicatesRemoved = duplicatesRemoved;
        InvalidRecords = invalidRecords;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(int totalLines, int readingsStored, int duplicatesRemoved, int invalidRecords)
    {
        TotalLines = totalLines;
        ReadingsStored = readingsStored;
        DuplicatesRemoved = duplicatesRemoved;
        InvalidRecords = invalidRecords;
        UpdatedAt = DateTime.UtcNow;
    }
}

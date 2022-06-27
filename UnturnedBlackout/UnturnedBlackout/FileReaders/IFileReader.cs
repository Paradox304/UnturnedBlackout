using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnturnedBlackout.FileReaders;
public interface IFileReader<out T> where T : class, new()
{
    public T FileData { get; }

    public string FilePath { get; }

    public void Load();

    public void Save();
}

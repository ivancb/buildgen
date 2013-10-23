using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Editor
{
    public interface IEditor
    {
        bool Edit(string contents);
    }
}

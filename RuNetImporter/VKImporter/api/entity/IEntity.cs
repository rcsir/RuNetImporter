using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public interface IEntity
    {
        String Name();
        String FileHeader();
        String ToFileLine();
    }
}

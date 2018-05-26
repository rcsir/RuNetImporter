using System;

namespace rcsir.net.vk.importer.api.entity
{
    public interface IEntity
    {
        String Name();
        String FileHeader();
        String ToFileLine();
    }
}

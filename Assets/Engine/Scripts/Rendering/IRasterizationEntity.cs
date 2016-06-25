using System.Collections.Generic;
using UnityEngine;

namespace Engine.Scripts.Rendering
{
    public interface IRasterizationEntity
    {
        //! Entity's boudning box vertices
        List<Vector3> BBoxVertices { get; set; }
        //! Entity's bounding box vertices transformed to screen space
        List<Vector3> BBoxVerticesTransformed { get; set; }
    }
}

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RLO.Science.BasicsOfProgramming
{
    public class Tile : SerializedMonoBehaviour
    {
        public Dictionary<DIR, Tile> Links;

        public Vector3 BotPos;

        [SerializeField] private MeshFilter _mFilter;
        [SerializeField] private MeshRenderer _mRenderer;
        [SerializeField] private Color _visitColor = Color.green;
        private Color defColor = Color.white;

        private bool _botPresent;

        void Start()
        {
            defColor = _mRenderer.materials[1].color;
        }

        public void OnBotVisit()
        {
            _mRenderer.materials[1].color = _visitColor;
        }

        public void OnBotLeave()
        {
            _mRenderer.materials[1].color = defColor;
        }

        public void DetectNeighbours()
        {
            Links = new Dictionary<DIR, Tile>
            {
                {DIR.North, null},
                {DIR.South, null},
                {DIR.East, null},
                {DIR.West, null}
            };
            
            var origin = transform.position;
            var n = new Ray(origin, Vector3.forward);
            var s = new Ray(origin, -Vector3.forward);
            var w = new Ray(origin, Vector3.left);
            var e = new Ray(origin, Vector3.right);
            Physics.Raycast(n, out var nHit, 1.0f);
            Physics.Raycast(s, out var sHit, 1.0f);
            Physics.Raycast(w, out var wHit, 1.0f);
            Physics.Raycast(e, out var eHit, 1.0f);

            if (nHit.transform != null) 
                Links[DIR.North] = nHit.transform.gameObject.GetComponent<Tile>();
            if (sHit.transform != null) 
                Links[DIR.South] = sHit.transform.gameObject.GetComponent<Tile>();
            if (eHit.transform != null) 
                Links[DIR.East] = eHit.transform.gameObject.GetComponent<Tile>();
            if (wHit.transform != null) 
                Links[DIR.West] = wHit.transform.gameObject.GetComponent<Tile>();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(BotPos, Vector3.one/10);

            var x = Vector3.up / 3;
            
            if (Links[DIR.North]) Gizmos.DrawLine(transform.position + x, Links[DIR.North].transform.position + x);
            if (Links[DIR.South]) Gizmos.DrawLine(transform.position + x, Links[DIR.South].transform.position + x);
            if (Links[DIR.East]) Gizmos.DrawLine(transform.position + x, Links[DIR.East].transform.position + x);
            if (Links[DIR.West]) Gizmos.DrawLine(transform.position + x, Links[DIR.West].transform.position + x);
        }

        private void SetBotPos()
        {
            var size = _mFilter.sharedMesh.bounds.size;
            var pos = transform.position;
            var scale = transform.localScale.x;
            pos.y = size.z / 2 * scale + pos.y; // we're using size.z due to model rotation.
            BotPos = pos;
        }

        void Reset()
        {
            DetectNeighbours();
            _mFilter = GetComponent<MeshFilter>();
            _mRenderer = GetComponent<MeshRenderer>();
            SetBotPos();
        }
    }

    public enum DIR { North, South, East, West}

}
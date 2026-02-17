using System.Collections.Generic;
using MBHS.Core.Utilities;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.BandManagement;
using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public class FieldRenderer : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField] private Material _fieldMaterial;
        [SerializeField] private Material _linesMaterial;

        [Header("Members")]
        [SerializeField] private float _memberHeight = 1.8f;
        [SerializeField] private float _memberRadius = 0.3f;

        private GameObject _fieldPlane;
        private GameObject _linesParent;
        private readonly Dictionary<string, GameObject> _memberObjects = new();
        private readonly Dictionary<InstrumentFamily, Color> _familyColors = new()
        {
            { InstrumentFamily.Brass, new Color(0.86f, 0.71f, 0.16f) },
            { InstrumentFamily.Woodwind, new Color(0.24f, 0.63f, 0.24f) },
            { InstrumentFamily.BatteryPercussion, new Color(0.78f, 0.24f, 0.24f) },
            { InstrumentFamily.FrontEnsemble, new Color(0.78f, 0.39f, 0.16f) },
            { InstrumentFamily.ColorGuard, new Color(0.63f, 0.24f, 0.78f) },
            { InstrumentFamily.Leadership, Color.white }
        };

        private IBandManager _bandManager;
        private Material _defaultLitMaterial;
        private Material _defaultUnlitMaterial;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Color_ID = Shader.PropertyToID("_Color");

        public void Initialize(IBandManager bandManager)
        {
            _bandManager = bandManager;
            CacheDefaultMaterials();
            CreateField();
            CreateFieldLines();
        }

        private void CacheDefaultMaterials()
        {
            // Get a URP-compatible material by grabbing it from a temp primitive.
            // This is the most reliable way - CreatePrimitive always gets a valid
            // material for the active render pipeline.
            var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _defaultLitMaterial = new Material(temp.GetComponent<Renderer>().sharedMaterial);
            DestroyImmediate(temp);

            // For unlit/line materials, use the built-in Sprites/Default which
            // works in all pipelines and supports vertex colors
            _defaultUnlitMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void UpdatePositions(List<MemberPosition> positions)
        {
            var toRemove = new HashSet<string>(_memberObjects.Keys);

            foreach (var pos in positions)
            {
                toRemove.Remove(pos.MemberId);

                if (!_memberObjects.TryGetValue(pos.MemberId, out var obj))
                {
                    obj = CreateMemberObject(pos.MemberId);
                    _memberObjects[pos.MemberId] = obj;
                }

                Vector3 worldPos = FieldCoordinates.FieldToWorld(pos.FieldPosition);
                worldPos.y = _memberHeight / 2f;
                obj.transform.localPosition = worldPos;
                obj.transform.localRotation = Quaternion.Euler(0f, pos.FacingAngle, 0f);
            }

            foreach (var id in toRemove)
            {
                if (_memberObjects.TryGetValue(id, out var obj))
                {
                    Destroy(obj);
                    _memberObjects.Remove(id);
                }
            }
        }

        public void Clear()
        {
            foreach (var obj in _memberObjects.Values)
                Destroy(obj);
            _memberObjects.Clear();
        }

        private void CreateField()
        {
            _fieldPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _fieldPlane.name = "FootballField";
            _fieldPlane.transform.SetParent(transform);
            _fieldPlane.transform.localPosition = Vector3.zero;

            float scaleX = FieldCoordinates.FieldLengthYards / 10f;
            float scaleZ = FieldCoordinates.FieldWidthYards / 10f;
            _fieldPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

            var renderer = _fieldPlane.GetComponent<Renderer>();
            if (_fieldMaterial != null)
            {
                renderer.material = _fieldMaterial;
            }
            else
            {
                renderer.material = CreateLitMaterial(new Color(0.13f, 0.55f, 0.13f));
            }
        }

        private void CreateFieldLines()
        {
            _linesParent = new GameObject("FieldLines");
            _linesParent.transform.SetParent(transform);
            _linesParent.transform.localPosition = Vector3.zero;

            float halfLength = FieldCoordinates.HalfFieldLength;
            float halfWidth = FieldCoordinates.HalfFieldWidth;
            float lineY = 0.02f;

            // Yard lines every 5 yards
            for (int yard = 0; yard <= 100; yard += 5)
            {
                float x = yard - halfLength;
                bool isMajor = yard % 10 == 0;

                var lineObj = new GameObject($"YardLine_{yard}");
                lineObj.transform.SetParent(_linesParent.transform);

                var lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(x, lineY, -halfWidth));
                lr.SetPosition(1, new Vector3(x, lineY, halfWidth));
                lr.startWidth = isMajor ? 0.15f : 0.08f;
                lr.endWidth = lr.startWidth;
                lr.useWorldSpace = false;

                if (_linesMaterial != null)
                {
                    lr.material = _linesMaterial;
                }
                else
                {
                    lr.material = CreateUnlitMaterial(isMajor
                        ? new Color(1f, 1f, 1f, 0.9f)
                        : new Color(1f, 1f, 1f, 0.5f));
                }
            }

            // Sidelines
            CreateBorderLine("HomeSideline",
                new Vector3(-halfLength, lineY, -halfWidth),
                new Vector3(halfLength, lineY, -halfWidth));
            CreateBorderLine("VisitorSideline",
                new Vector3(-halfLength, lineY, halfWidth),
                new Vector3(halfLength, lineY, halfWidth));
            CreateBorderLine("HomeEndline",
                new Vector3(-halfLength, lineY, -halfWidth),
                new Vector3(-halfLength, lineY, halfWidth));
            CreateBorderLine("VisitorEndline",
                new Vector3(halfLength, lineY, -halfWidth),
                new Vector3(halfLength, lineY, halfWidth));

            // Hash marks
            float homeHash = FieldCoordinates.HomeHashYards - halfWidth;
            float visitorHash = FieldCoordinates.VisitorHashYards - halfWidth;

            for (int yard = 0; yard <= 100; yard++)
            {
                float x = yard - halfLength;
                CreateHashMark($"HomeHash_{yard}", x, homeHash, lineY);
                CreateHashMark($"VisitorHash_{yard}", x, visitorHash, lineY);
            }
        }

        private void CreateBorderLine(string name, Vector3 start, Vector3 end)
        {
            var lineObj = new GameObject(name);
            lineObj.transform.SetParent(_linesParent.transform);

            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.useWorldSpace = false;

            lr.material = _linesMaterial != null
                ? _linesMaterial
                : CreateUnlitMaterial(Color.white);
        }

        private void CreateHashMark(string name, float x, float z, float y)
        {
            var lineObj = new GameObject(name);
            lineObj.transform.SetParent(_linesParent.transform);

            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x - 0.3f, y, z));
            lr.SetPosition(1, new Vector3(x + 0.3f, y, z));
            lr.startWidth = 0.06f;
            lr.endWidth = 0.06f;
            lr.useWorldSpace = false;

            lr.material = _linesMaterial != null
                ? _linesMaterial
                : CreateUnlitMaterial(new Color(1f, 1f, 1f, 0.4f));
        }

        private GameObject CreateMemberObject(string memberId)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = $"Member_{memberId[..Mathf.Min(8, memberId.Length)]}";
            obj.transform.SetParent(transform);
            obj.transform.localScale = new Vector3(
                _memberRadius * 2f, _memberHeight / 2f, _memberRadius * 2f);

            var renderer = obj.GetComponent<Renderer>();
            var member = _bandManager?.Roster?.GetMemberById(memberId);
            Color color = Color.gray;

            if (member != null)
            {
                var family = GetFamily(member.AssignedInstrument);
                _familyColors.TryGetValue(family, out color);
            }

            renderer.material = CreateLitMaterial(color);

            // Remove collider (not needed for preview)
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Facing indicator
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "FacingIndicator";
            indicator.transform.SetParent(obj.transform);
            indicator.transform.localPosition = new Vector3(0f, 0.6f, 0.8f);
            indicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.5f);

            var indRenderer = indicator.GetComponent<Renderer>();
            indRenderer.material = CreateLitMaterial(Color.white);

            var indCollider = indicator.GetComponent<Collider>();
            if (indCollider != null)
                Destroy(indCollider);

            return obj;
        }

        // =====================================================================
        // Material Helpers
        // =====================================================================

        private Material CreateLitMaterial(Color color)
        {
            var mat = new Material(_defaultLitMaterial);
            // Try URP property name first, fall back to standard
            if (mat.HasProperty(BaseColor))
                mat.SetColor(BaseColor, color);
            else
                mat.color = color;
            return mat;
        }

        private Material CreateUnlitMaterial(Color color)
        {
            var mat = new Material(_defaultUnlitMaterial);
            mat.color = color;
            return mat;
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static InstrumentFamily GetFamily(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.Trumpet or InstrumentType.Trombone or
                InstrumentType.FrenchHorn or InstrumentType.Tuba or
                InstrumentType.Sousaphone or InstrumentType.Baritone or
                InstrumentType.Mellophone => InstrumentFamily.Brass,

                InstrumentType.Flute or InstrumentType.Piccolo or
                InstrumentType.Clarinet or InstrumentType.Saxophone
                    => InstrumentFamily.Woodwind,

                InstrumentType.SnareDrum or InstrumentType.BassDrum or
                InstrumentType.TenorDrums or InstrumentType.Cymbals
                    => InstrumentFamily.BatteryPercussion,

                InstrumentType.Xylophone or InstrumentType.Marimba or
                InstrumentType.Vibraphone or InstrumentType.Timpani
                    => InstrumentFamily.FrontEnsemble,

                InstrumentType.Flag or InstrumentType.Rifle or
                InstrumentType.Saber => InstrumentFamily.ColorGuard,

                InstrumentType.DrumMajor => InstrumentFamily.Leadership,

                _ => InstrumentFamily.Brass
            };
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}

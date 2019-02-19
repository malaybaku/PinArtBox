using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloPlay;

namespace Baku.Laquarium
{
    public class WebCamPinArt : MonoBehaviour
    {
        #region From Inspector

        [SerializeField]
        private string deviceName = "";

        [SerializeField]
        private int requestWidth = 320;

        [SerializeField]
        private int requestHeight = 240;

        [SerializeField]
        Vector3 boundingBox1 = Vector3.zero;

        [SerializeField]
        Vector3 boundingBox2 = Vector3.zero;

        [SerializeField]
        int horizontalDivision = 60;

        [SerializeField]
        int verticalDivision = 40;

        [SerializeField]
        Transform barPrefab = null;

        [SerializeField]
        float lerpFactor = 0.1f;

        [SerializeField]
        Vector3 initialScaleFactor = Vector3.one;

        #endregion

        private Transform[] _allBar = null;
        private Material[] _allMaterial = null;
        private WebCamTexture _webCamTexture = null;

        private float counter = 0.1f;

        private int _scaleMode = ScaleModes.Gray;
        private bool _useNegaScale = false;

        //note: we should avoid software rendering in this case but the focus is concept validation!!
        private void Start()
        {
            SetupBar();
            SetupMaterial();
            StartCoroutine(SetupCamera());
        }

        private void Update()
        {
            if (_webCamTexture == null) { return; }

            //switch color mode by the buttons on LKG or keyboard L/R arrow 
            if (Buttons.GetButtonDown(ButtonType.RIGHT) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                _scaleMode++;
                if (_scaleMode > ScaleModes.S)
                {
                    _scaleMode = ScaleModes.Gray;
                    _useNegaScale = !_useNegaScale;
                }
            }
            
            if (Buttons.GetButtonDown(ButtonType.LEFT) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _scaleMode--;
                if (_scaleMode < ScaleModes.Gray)
                {
                    _scaleMode = ScaleModes.S;
                    _useNegaScale = !_useNegaScale;
                }
            }

            //move the pins
            UpdateBarPositions();
        }

        IEnumerator SetupCamera()
        {
            yield return new WaitForSeconds(0.5f);

            var devices = WebCamTexture.devices;

            if (devices == null || devices.Length == 0)
            {
                yield break;
            }

            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"WebCam {i}:{devices[i].name}");
                Debug.Log($"WebCam {i} (depth):{devices[i].depthCameraName}");
            }

            string requestDeviceName =
                devices.Any(d => d.name == deviceName) ?
                deviceName :
                devices[0].name;

            _webCamTexture = new WebCamTexture(requestDeviceName, requestWidth, requestHeight);
            _webCamTexture.Play();
        }

        //Creates all bars to show
        void SetupBar()
        {
            if (barPrefab == null)
            {
                return;
            }

            float hInterval = (boundingBox2.x - boundingBox1.x) / horizontalDivision;
            float vInterval = (boundingBox2.z - boundingBox1.z) / verticalDivision;

            var allBarList = new List<Transform>();
            for (int h = 0; h < horizontalDivision; h++)
            {
                for (int v = 0; v < verticalDivision; v++)
                {
                    var offset = new Vector3(hInterval * h, 0, vInterval * v);

                    var item = Instantiate(
                        barPrefab,
                        transform
                        );

                    item.localPosition = boundingBox1 + offset;

                    item.localScale =new Vector3(
                        hInterval * initialScaleFactor.x, 
                        boundingBox2.y * initialScaleFactor.y, 
                        vInterval * initialScaleFactor.z
                        );

                    allBarList.Add(item);
                }
            }

            _allBar = allBarList.ToArray();
        }

        //Get materials, to change their color
        void SetupMaterial()
        {
            _allMaterial = new Material[_allBar.Length];
            for (int i = 0; i < _allBar.Length; i++)
            {
                _allMaterial[i] = _allBar[i].GetComponent<MeshRenderer>().material;
            }
        }

        void UpdateBarPositions()
        {
            float hCoef = _webCamTexture.width * 1f / horizontalDivision;
            float vCoef = _webCamTexture.height * 1f / verticalDivision;
            float yMax = boundingBox2.y * 0.5f;
            float yMin = boundingBox1.y - yMax;
            float yCoef = yMax - yMin;
            float yCoefInv = 1.0f / yCoef;

            var colors = _webCamTexture.GetPixels();

            int h = 0;
            int v = 0;
            for (int i = 0; i < _allBar.Length; i++)
            {
                var bar = _allBar[i];
                var color = colors[(int)(v * vCoef) * _webCamTexture.width + _webCamTexture.width - 1 - (int)(h * hCoef)];

                float scale = 0f;
                if (_useNegaScale)
                {
                    scale = Mathf.Lerp(
                        (bar.localPosition.y - yMin) * yCoefInv,
                        1 - ScaleModes.GetScale(color, _scaleMode),
                        lerpFactor
                        );
                }
                else
                {
                    scale = Mathf.Lerp(
                        (bar.localPosition.y - yMin) * yCoefInv,
                        ScaleModes.GetScale(color, _scaleMode),
                        lerpFactor
                        );
                }

                bar.localPosition = new Vector3(
                    bar.localPosition.x,
                    yMin + scale * yCoef,
                    bar.localPosition.z
                    );

                _allMaterial[i].color = Color.Lerp(
                    _allMaterial[i].color,
                    ScaleModes.GetColor(color, _scaleMode),
                    lerpFactor
                    );

                v++;
                if (!(v - verticalDivision < 0))
                {
                    v = 0;
                    h++;
                }
            }

        }
    
        /// <summary>
        /// Defines how to translate pixel color to output color and scale (0 to 1) value
        /// </summary>
        static class ScaleModes
        {
            public const int Gray = 0;
            public const int R = 1;
            public const int G = 2;
            public const int B = 3;
            public const int H = 4;
            public const int S = 5;
            //we do not need V because gray means almost same
            //public const int V = 6;

            const float HCoef = 1.0f / 360.0f;
            const float GrayCoef = 1.0f / 3.0f;

            public static float GetScale(Color src, int mode)
            {
                switch (mode)
                {
                    case R:
                        return src.r;
                    case G:
                        return src.g;
                    case B:
                        return src.b;
                    //case V:
                    //    return src.maxColorComponent;
                    case S:
                        float max = src.maxColorComponent;
                        if (max == 0)
                        {
                            return 0;
                        }
                        else
                        {
                            float min = (src.r < src.g ?
                                Mathf.Min(src.r, src.b) :
                                Mathf.Min(src.g, src.b)
                                );
                            return (max - min) / max;
                        }
                    case H:
                        float hmax = src.maxColorComponent;
                        float hmin = (src.r < src.g ?
                            Mathf.Min(src.r, src.b) :
                            Mathf.Min(src.g, src.b)
                            );
                        if (hmax == hmin)
                        {
                            return 0;
                        }
                        else if (hmin == src.b)
                        {
                            return (60f * (src.g - src.r) / (hmax - hmin) + 60) * HCoef;
                        }
                        else if (hmin == src.r)
                        {
                            return (60f * (src.b - src.g) / (hmax - hmin) + 180) * HCoef;
                        }
                        else // (hmin == src.g)
                        {
                            return (60f * (src.r - src.b) / (hmax - hmin) + 300) * HCoef;
                        }
                    case Gray:
                    default:
                        return src.grayscale;
                }

            }

            public static Color GetColor(Color src, int mode)
            {
                switch (mode)
                {
                    case R:
                        return new Color(src.r, 0, 0);
                    case G:
                        return new Color(0, src.g, 0);
                    case B:
                        return new Color(0, 0, src.b);
                    case H:
                        return Color.HSVToRGB(GetScale(src, H), 1, 1);
                    case S:
                        return Color.HSVToRGB(0, GetScale(src, S), 1);
                    //case V:
                    case Gray:
                    default:
                        return new Color(src.grayscale, src.grayscale, src.grayscale);
                }


            }
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCH.AutoFill.Service;
using ISS.OpticalCharacterRecognition;

namespace DocumentOCR
{
    public class OCRWrapper
    {
        #region singleton stuff

        private static OCRWrapper _instance;

        private OCRWrapper()
        { }

        public static OCRWrapper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new OCRWrapper();

                return _instance;
            }
        }

        #endregion

        private AbstractOCREngine _ocrEngine;

        protected AbstractOCREngine OcrEngine
        {
            get
            {
                if (_ocrEngine == null)
                {
                    try
                    {
                        LastOCRException = null;

                        _ocrEngine = new AbbyyOCREngine();
                    }
                    catch (Exception ex)
                    {
                        LastOCRException = ex;
                    }
                }

                return _ocrEngine;
            }
        }

        public Exception LastOCRException { get; private set; }


        public bool PerformOcr(string filePath, string outputPath)
        {
            //System.IO.File.Copy(filePath, outputPath,true); return true;

            if (OcrEngine != null)
            {
                try
                {
                    OcrEngine.InitDocument();

                    OcrEngine.Process(new List<string>() { filePath }, false);

                    OcrEngine.ExportToPDF(outputPath);

                    return true;
                }
                catch (Exception ex)
                {
                    LastOCRException = ex;
                }
            }
            
            return false;
        }

    }
}

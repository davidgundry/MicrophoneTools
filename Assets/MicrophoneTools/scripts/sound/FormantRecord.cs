namespace MicTools
{
    public class FormantRecord
    {

        private int lowerBound;
        public int LowerBound
        {
            get
            {
                return lowerBound;
            }
        }
        private int higherBound;
        public int HigherBound
        {
            get
            {
                return higherBound;
            }
        }
        private int peak;
        public int Peak
        {
            get
            {
                return peak;
            }
        }

        public FormantRecord(int lowerBound, int higherBound, int peak)
        {
            this.lowerBound = lowerBound;
            this.higherBound = higherBound;
            this.peak = peak;
        }

        public float PeakFrequency
        {
            get
            {
                return FFTPitchDetector.IndexToFrequency(peak);
            }
        }
    }
}
namespace MicTools
{
    public class VowelRecord
    {
        private readonly string vowel;
        private readonly int f1;
        public int F1
        {
            get
            {
                return f1;
            }
        }
        private readonly int f2;
        public int F2
        {
            get
            {
                return f2;
            }
        }

        public VowelRecord(string vowel, int f1, int f2)
        {
            this.vowel = vowel;
            this.f1 = f1;
            this.f2 = f2;
        }

        public override string ToString()
        {
            return vowel;
        }

    }
}
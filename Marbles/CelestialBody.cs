namespace Marbles
{
    class CelestialBody
    {
        public string Name { get; }
        public Texture TextureMap { get; }
        public Texture SpecularMap { get; }
        public Texture RingTexture { get; }
        public float SpecularPower { get; }
        public int StarIndex { get; }

        public CelestialBody(string name, Texture texture, Texture specularMap = null, Texture ringTexture = null, int starIndex = -1, float specularPower = -1f)
        {
            Name = name;
            TextureMap = texture;
            SpecularMap = specularMap;
            RingTexture = ringTexture;
            SpecularPower = specularPower;
            StarIndex = starIndex;
        }
    }
}

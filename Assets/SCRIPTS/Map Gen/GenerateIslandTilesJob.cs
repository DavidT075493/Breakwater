using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateIslandTilesJob : IJobParallelFor
{
    public int mapSize;

    [ReadOnly] public NativeArray<byte> sourceGround;
    [ReadOnly] public NativeArray<byte> sourceWater;
    [ReadOnly] public NativeArray<byte> sourceHill;
    [ReadOnly] public NativeArray<byte> sourceRoof;
    [ReadOnly] public NativeArray<byte> sourceLava;

    public NativeArray<byte> outGround;
    public NativeArray<byte> outWater;
    public NativeArray<byte> outHill;
    public NativeArray<byte> outRoof;
    public NativeArray<byte> outLava;
    public NativeArray<byte> outLavaCollision;

    public void Execute(int index)
    {
        byte g = sourceGround[index];
        byte w = sourceWater[index];
        byte h = sourceHill[index];
        byte r = sourceRoof[index];
        byte l = sourceLava[index];

        outGround[index] = g;
        outWater[index] = w;
        outHill[index] = h;
        outRoof[index] = r;
        outLava[index] = l;

        outLavaCollision[index] = (l != 0) ? (byte)1 : (byte)0;
    }
}
using UnityEngine;

public class VoxelGrid
{
    public class Voxel
    {
        public Vector3 position; //Center position of the voxel
        public float density;

        public Voxel(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
            density = 0;
        }

        public void setLinearDensity(float distance, float ks)
        {
            density = (distance < ks) ? 1.0f - (distance / ks): 0;
        }
    }

    public int numPerDim; // The number of voxel per dimension in the grid
    public float lengthPerDim; // Length of the grid in each dimension
    public Voxel[,,] voxels;

    public VoxelGrid(int nPerDim, float lPerDim)
    {
        numPerDim = nPerDim;
        lengthPerDim = lPerDim;

        // Initialize the voxel grid and voxels
        voxels = new Voxel[numPerDim, numPerDim, numPerDim];
        float voxelLength = lPerDim / (float) nPerDim;
        for (int x = 0; x < numPerDim; x++)
        {
            for (int y = 0; y < numPerDim; y++)
            {
                for (int z = 0; z < numPerDim; z++)
                {
                    float hl = voxelLength / 2.0f;
                    voxels[x, y, z] = new Voxel(hl + voxelLength * x, hl + voxelLength * y, hl + voxelLength * z);
                }
            }
        }
    }

    public void add(VoxelGrid B)
    {
        if (B.numPerDim != numPerDim || B.lengthPerDim != lengthPerDim)
        {
            Debug.LogError("Tried to sum two grids with different dimensions or lengths! This is not supported");
            return;
        }
        
        for (int x = 0; x < numPerDim; x++)
        {
            for (int y = 0; y < numPerDim; y++)
            {
                for (int z = 0; z < numPerDim; z++)
                {
                    voxels[x, y, z].density += B.voxels[x, y, z].density;
                }
            }
        }
    }

    public void normalizeDensities(int n)
    {
        for (int x = 0; x < numPerDim; x++)
        {
            for (int y = 0; y < numPerDim; y++)
            {
                for (int z = 0; z < numPerDim; z++)
                {
                    voxels[x, y, z].density = voxels[x, y, z].density / n;
                }
            }
        }
    }

    public void setDensities(int d)
    {
        for (int x = 0; x < numPerDim; x++)
        {
            for (int y = 0; y < numPerDim; y++)
            {
                for (int z = 0; z < numPerDim; z++)
                {
                    voxels[x, y, z].density = d;
                }
            }
        }
    }

    public void print()
    {
        for (int x = 0; x < numPerDim; x++)
        {
            for (int y = 0; y < numPerDim; y++)
            {
                string zLine = "";
                for (int z = 0; z < numPerDim; z++)
                {
                    zLine += ", " + voxels[x, y, z].density;
                }

                Debug.Log("(x: " + x + ", y: " + y + ", ...): " + zLine);
            }
        }
    }
}

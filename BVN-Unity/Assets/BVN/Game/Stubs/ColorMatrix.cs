using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Minimal port of fl.motion.ColorMatrix (LIB_Other, not assigned to any porter):
    /// a 5x5 colour matrix with brightness / contrast / saturation setters and GetFlatArray.
    /// </summary>
    public class ColorMatrix
    {
        protected const double LUMINANCER = 0.3086;
        protected const double LUMINANCEG = 0.6094;
        protected const double LUMINANCEB = 0.0820;

        protected double[,] m_matrix = new double[5, 5];

        public ColorMatrix()
        {
            LoadIdentity();
        }

        public virtual void LoadIdentity()
        {
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    m_matrix[i, j] = i == j ? 1 : 0;
        }

        public virtual double GetValue(int row, int col) => m_matrix[row, col];
        public virtual void SetValue(int row, int col, double value) => m_matrix[row, col] = value;

        public virtual void SetBrightnessMatrix(double value)
        {
            if (m_matrix == null) return;
            m_matrix[0, 4] = value;
            m_matrix[1, 4] = value;
            m_matrix[2, 4] = value;
        }

        public virtual void SetContrastMatrix(double value)
        {
            if (m_matrix == null) return;
            double brightness = 0.5 * (127.0 - value);
            value = value / 127.0;
            m_matrix[0, 0] = value;
            m_matrix[1, 1] = value;
            m_matrix[2, 2] = value;
            m_matrix[0, 4] = brightness;
            m_matrix[1, 4] = brightness;
            m_matrix[2, 4] = brightness;
        }

        public virtual void SetSaturationMatrix(double value)
        {
            if (m_matrix == null) return;
            double subVal = 1.0 - value;

            double mulVal = subVal * LUMINANCER;
            m_matrix[0, 0] = mulVal + value;
            m_matrix[1, 0] = mulVal;
            m_matrix[2, 0] = mulVal;

            mulVal = subVal * LUMINANCEG;
            m_matrix[0, 1] = mulVal;
            m_matrix[1, 1] = mulVal + value;
            m_matrix[2, 1] = mulVal;

            mulVal = subVal * LUMINANCEB;
            m_matrix[0, 2] = mulVal;
            m_matrix[1, 2] = mulVal;
            m_matrix[2, 2] = mulVal + value;
        }

        /// <summary>Flat array of 20 values (rows 0-3, columns 0-4) for ColorMatrixFilter.</summary>
        public virtual List<object> GetFlatArray()
        {
            if (m_matrix == null) return null;
            var ptr = new List<object>(20);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 5; j++)
                    ptr.Add(m_matrix[i, j]);
            return ptr;
        }
    }
}

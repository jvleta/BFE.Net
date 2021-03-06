﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Integration;
using BriefFiniteElementNet.Loads;

namespace BriefFiniteElementNet.ElementHelpers
{
    /// <summary>
    /// Represents a helper for shaft (torsion element).
    /// </summary>
    public class ShaftHelper : IElementHelper
    {
        public Element TargetElement { get; set; }

        /// <inheritdoc/>
        public Matrix GetBMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var l = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var buf = new Matrix(1, 2);

            var b1 = -1 / l;
            var b2 = 1 / l;

            var c1 = elm.StartReleaseCondition;
            var c2 = elm.EndReleaseCondition;

            if (c1.RX == DofConstraint.Released)
                b1 = 0;

            if (c2.RX == DofConstraint.Released)
                b2 = 0;

            buf.FillRow(0, b1, b2);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetB_iMatrixAt(Element targetElement, int i, params double[] isoCoords)
        {
            if (i != 0)
                throw new Exception();

            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var l = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var buf = new Matrix(1, 2);

            buf.FillRow(0, -1 / l, 1 / l);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetDMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            ThrowUtil.ThrowIf(!CalcUtil.IsIsotropicMaterial(mech), "anistropic material not impolemented yet");

            var g = mech.Ex / (2 * (1 + mech.NuXy));

            buf.FillRow(0, geo.J * g);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetRhoMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            buf[0, 0] = geo.J * mech.Rho;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetMuMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            var geo = elm.Section.GetCrossSectionPropertiesAt(xi);
            var mech = elm.Material.GetMaterialPropertiesAt(xi);

            var buf = new Matrix(1, 1);

            buf[0, 0] = geo.A * mech.Mu;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetNMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var n1 = 1 / 2.0 - xi / 2;
            var n2 = 1 / 2.0 + xi / 2;


            var c1 = bar.StartReleaseCondition;
            var c2 = bar.EndReleaseCondition;

            if (c1.RX == DofConstraint.Released)
                n1 = 0;

            if (c2.RX == DofConstraint.Released)
                n2 = 0;


            var buf = new Matrix(1, 2);

            double[] arr;

            arr = new double[] {n1, n2};

            buf.FillRow(0, arr);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetJMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var buf = new Matrix(1, 1);

            buf[0, 0] = (bar.EndNode.Location - bar.StartNode.Location).Length / 2;

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalKMatrix(Element targetElement)
        {
            return ElementHelperExtensions.CalcLocalKMatrix_Bar(this, targetElement);
        }

        /// <inheritdoc/>
        public Matrix CalcLocalMMatrix(Element targetElement)
        {
            var buf = ElementHelperExtensions.CalcLocalMMatrix_Bar(this, targetElement);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix CalcLocalCMatrix(Element targetElement)
        {
            return ElementHelperExtensions.CalcLocalCMatrix_Bar(this, targetElement);
        }

        /// <inheritdoc/>
        public FluentElementPermuteManager.ElementLocalDof[] GetDofOrder(Element targetElement)
        {
            return new FluentElementPermuteManager.ElementLocalDof[]
           {
                new FluentElementPermuteManager.ElementLocalDof(0, DoF.Rx),
                new FluentElementPermuteManager.ElementLocalDof(1, DoF.Rx),
           }; 
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<DoF, double>> GetLocalInternalForceAt(Element targetElement,
            Displacement[] localDisplacements, params double[] isoCoords)
        {
            var ld = localDisplacements;

            var b = GetBMatrixAt(targetElement, isoCoords);
            var d = GetDMatrixAt(targetElement, isoCoords);
            var u = new Matrix(2, 1);

            u.FillColumn(0, ld[0].RX, ld[1].RX);

            var frc = d * b * u;

            var buf = new List<Tuple<DoF, double>>();

            buf.Add(Tuple.Create(DoF.Rx, frc[0, 0]));

            return buf;
        }

        /// <inheritdoc/>
        public bool DoesOverrideKMatrixCalculation(Element targetElement, Matrix transformMatrix)
        {
            return false;
        }

        /// <inheritdoc/>
        public int[] GetNMaxOrder(Element targetElement)
        {
            return new int[] {1, 0, 0};
        }

        public int[] GetBMaxOrder(Element targetElement)
        {
            return new int[] { 0, 0, 0 };
        }

        public int[] GetDetJOrder(Element targetElement)
        {
            return new int[] { 0, 0, 0 };
        }

        public IEnumerable<Tuple<DoF, double>> GetLoadInternalForceAt(Element targetElement, Load load,
            double[] isoLocation)
        {
                throw new NotImplementedException();

        }

        public Displacement GetLoadDisplacementAt(Element targetElement, Load load, double[] isoLocation)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Displacement GetLocalDisplacementAt(Element targetElement, Displacement[] localDisplacements, params double[] isoCoords)
        {
            var n = GetNMatrixAt(targetElement, isoCoords);
            var u = new Matrix(2, 1);

            u[0, 0] = localDisplacements[0].RX;
            u[1, 0] = localDisplacements[1].RX;

            var buf = n * u;

            return new Displacement(0, 0, 0, buf[0, 0], 0, 0);
        }

        public Force[] GetLocalEquivalentNodalLoads(Element targetElement, Load load)
        {
            if (load is UniformLoad)
            {
                return new Force[2];
            }

            if (load is PartialTrapezoidalLoad)
            {
                return new Force[2];
            }

            throw new NotImplementedException();
        }
    }
}
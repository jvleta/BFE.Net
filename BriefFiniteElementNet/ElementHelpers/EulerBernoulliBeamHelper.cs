﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Integration;
using BriefFiniteElementNet.Loads;
using ElementLocalDof = BriefFiniteElementNet.FluentElementPermuteManager.ElementLocalDof;

namespace BriefFiniteElementNet.ElementHelpers
{
    /// <summary>
    /// Represents a helper class for Euler - Bernoulli beam element
    /// </summary>
    public class EulerBernoulliBeamHelper : IElementHelper
    {
        private BeamDirection _direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="EulerBernoulliBeamHelper"/> class.
        /// </summary>
        /// <param name="direction">The direction.</param>
        public EulerBernoulliBeamHelper(BeamDirection direction)
        {
            _direction = direction;
        }

        public Element TargetElement { get; set; }

        /// <inheritdoc/>
        public Matrix GetBMatrixAt(Element targetElement, params double[] isoCoords)
        {
            //TODO: Take end supports into consideration

            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var L = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var L2 = L*L;

            var buf = new Matrix(1, 4);

            double[] arr;

            if (_direction == BeamDirection.Z)
                arr = new double[] {-(6*xi)/L2, (3*xi)/L - 1/L, +(6*xi)/L2, (3*xi)/L + 1/L};
            else
                arr = new double[] {(6*xi)/L2, (3*xi)/L - 1/L, -(6*xi)/L2, (3*xi)/L + 1/L};

            buf.FillRow(0, arr);

            return buf;
        }

        /// <inheritdoc/>
        public Matrix GetB_iMatrixAt(Element targetElement, int i, params double[] isoCoords)
        {
            var elm = targetElement as BarElement;

            if (elm == null)
                throw new Exception();

            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var L = (elm.EndNode.Location - elm.StartNode.Location).Length;

            var L2 = L * L;

            double bufVal;

            switch(i)
            {
                case 0:
                    bufVal = _direction == BeamDirection.Z ? -(6 * xi) / L2 : -(6 * xi) / L2;
                    break;

                case 1:
                    bufVal = (3 * xi) / L - 1 / L;
                    break;

                case 2:
                    bufVal = _direction == BeamDirection.Z ? +(6 * xi) / L2 : -(6 * xi) / L2;
                    break;

                case 3:
                    bufVal = (3 * xi) / L + 1 / L;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var buf = new Matrix(1, 1);

            buf.SetMember(0, 0, bufVal);

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

            var ei = 0.0;

            if (_direction == BeamDirection.Y)
                ei = geo.Iy * mech.Ex;
            else
                ei = geo.Iz * mech.Ex;

            buf[0, 0] = ei;

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

            buf[0, 0] = geo.A*mech.Rho;

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
            if (targetElement is BarElement)
                return GetNMatrixBar2Node(targetElement, isoCoords);

            throw new NotImplementedException();
        }

        public Matrix GetNMatrixBar2Node(Element targetElement, params double[] isoCoords)
        {
            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var L = (bar.EndNode.Location - bar.StartNode.Location).Length;

            var n1s = new double[] //N1,N1', N1'', N1'''
            {
                1.0 / 4.0 * (1 - xi) * (1 - xi) * (2 + xi),
                1.0 / 4.0 * (3 * xi * xi - 3),
                1.0 / 4.0 * (6 * xi),
                1.0 / 4.0 * (6)
            };

            var m1s = new double[] //M1,M1', M1'', M1'''
            {
                L / 8.0 * (1 - xi) * (1 - xi) * (xi + 1),
                L / 8.0 * (3 * xi * xi - 2 * xi - 1),
                L / 8.0 * (6 * xi - 2.0),
                L / 8.0 * (6),
            };

            var n2s = new double[] //N2,N2', N2'', N2'''
            {
                1.0 / 4.0 * (1 + xi)*(1 + xi)*(2 - xi),
                1.0 / 4.0 * (-3 * xi * xi + 3),
                1.0 / 4.0 * (-6 * xi),
                1.0 / 4.0 * (-6)
            };

            var m2s = new double[] //M1,M1', M1'', M1'''
            {
                L / 8.0 * (1 + xi)*(1 + xi)*(xi - 1),
                L / 8.0 * (3 * xi * xi + 2 * xi - 1),
                L / 8.0 * (6 * xi + 2.0),
                L / 8.0 * (6),
            };

            var buf2 = new Matrix(4, 4);


            var c1 = bar.StartReleaseCondition;
            var c2 = bar.EndReleaseCondition;

            if (_direction == BeamDirection.Z)
            {
                if (c1.DY == DofConstraint.Released)
                    n1s.FillWith(0);

                if (c1.RZ == DofConstraint.Released)
                    m1s.FillWith(0);

                if (c2.DY == DofConstraint.Released)
                    n2s.FillWith(0);

                if (c2.RZ == DofConstraint.Released)
                    m2s.FillWith(0);
            }
            else
            {
                m1s = m1s.Negate();
                m2s = m2s.Negate();

                if (c1.DZ == DofConstraint.Released)
                    n1s.FillWith(0);

                if (c1.RY == DofConstraint.Released)
                    m1s.FillWith(0);

                if (c2.DZ == DofConstraint.Released)
                    n2s.FillWith(0);

                if (c2.RY == DofConstraint.Released)
                    m2s.FillWith(0);
            }


            buf2.FillColumn(0, n1s);
            buf2.FillColumn(1, m1s);
            buf2.FillColumn(2, n2s);
            buf2.FillColumn(3, m2s);

            return buf2;
        }

        /// <inheritdoc/>
        public Matrix GetJMatrixAt(Element targetElement, params double[] isoCoords)
        {
            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var buf = new Matrix(1, 1);

            buf[0, 0] = (bar.EndNode.Location - bar.StartNode.Location).Length/2;

            return buf;
        }

        public double[] Iso2Local(Element targetElement, params double[] isoCoords)
        {
            var tg = targetElement as BarElement;


            if (tg != null)
            {
                var xi = isoCoords[0];

                if (tg.Nodes.Length == 2)
                {
                    var l = (tg.Nodes[1].Location - tg.Nodes[0].Location).Length;
                    return new[] { l * (xi + 1) / 2 };
                }
            }

            throw new NotImplementedException();
        }

        public double[] Local2Iso(Element targetElement, params double[] localCoords)
        {
            var tg = targetElement as BarElement;


            if (tg != null)
            {
                var x = localCoords[0];

                if (tg.Nodes.Length == 2)
                {
                    var l = (tg.Nodes[1].Location - tg.Nodes[0].Location).Length;
                    return new[] { 2 * x / l - 1 };
                }
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Matrix CalcLocalKMatrix(Element targetElement)
        {
            var buf = ElementHelperExtensions.CalcLocalKMatrix_Bar(this, targetElement);

            return buf;
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
                new FluentElementPermuteManager.ElementLocalDof(0, _direction == BeamDirection.Y ? DoF.Dy : DoF.Dz),
                new FluentElementPermuteManager.ElementLocalDof(0, _direction == BeamDirection.Y ? DoF.Rz : DoF.Ry),
                new FluentElementPermuteManager.ElementLocalDof(1, _direction == BeamDirection.Y ? DoF.Dy : DoF.Dz),
                new FluentElementPermuteManager.ElementLocalDof(1, _direction == BeamDirection.Y ? DoF.Rz : DoF.Ry),
            };
        }

        /// <inheritdoc/>
        public bool DoesOverrideKMatrixCalculation(Element targetElement, Matrix transformMatrix)
        {
            return false;
        }

        /// <inheritdoc/>
        public int[] GetNMaxOrder(Element targetElement)
        {
            return new int[] {3, 0, 0};
        }

        public int[] GetBMaxOrder(Element targetElement)
        {
            return new[] {1,0,0};
        }

        public int[] GetDetJOrder(Element targetElement)
        {
            return new int[] {0, 0, 0};
        }


        /// <inheritdoc/>
        public IEnumerable<Tuple<DoF, double>> GetLoadInternalForceAt(Element targetElement, Load load,
            double[] isoLocation)
        {
            var buf = new FlatShellStressTensor();
            
            var tr = targetElement.GetTransformationManager();

            var br = targetElement as BarElement;

            var endForces = GetLocalEquivalentNodalLoads(targetElement, load);


            var v0 =
                this._direction == BeamDirection.Z ?
                endForces[0].Fy : endForces[0].Fz;

            var m0 = this._direction == BeamDirection.Z ?
                endForces[0].Mz : endForces[0].My;

            v0 = -v0;
            m0 = -m0;

            var to = Iso2Local(targetElement, isoLocation)[0];

            //var xi = isoLocation[0];

            #region uniform & trapezoid

            if (load is UniformLoad || load is PartialTrapezoidalLoad)
            {

                Func<double, double> magnitude;
                Vector localDir;

                double xi0, xi1;
                int degree;//polynomial degree of magnitude function

                #region inits
                if (load is UniformLoad)
                {
                    var uld = (load as UniformLoad);

                    magnitude = (xi => uld.Magnitude);
                    localDir = uld.Direction;

                    if (uld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = -1;
                    xi1 = 1;
                    degree = 0;
                }
                else
                {
                    var tld = (load as PartialTrapezoidalLoad);

                    magnitude = (xi => (load as PartialTrapezoidalLoad).GetMagnitudesAt(xi, 0, 0)[0]);
                    localDir = tld.Direction;

                    if (tld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = tld.StarIsoLocations[0];
                    xi1 = tld.EndIsoLocations[0];
                    degree = 1;
                }

                localDir = localDir.GetUnit();
                #endregion

                {

                    var nOrd = 0;// GetNMaxOrder(targetElement).Max();

                    var gpt = (nOrd + degree) / 2 + 1;//gauss point count

                    Matrix integral;


                    if(isoLocation[0]<xi0)
                    {
                        integral = new Matrix(2, 1);
                    }
                    else
                    {
                        var intgV = GaussianIntegrator.CreateFor1DProblem(x =>
                        {
                            var xi = Local2Iso(targetElement, x)[0];
                            var q__ = magnitude(xi);
                            var q_ = localDir * q__;

                            double df, dm;

                            if (this._direction == BeamDirection.Y)
                            {
                                df = q_.Z;
                                dm = -q_.Z * x;
                            }
                            else
                            {
                                df = q_.Y;
                                dm = q_.Y * x;
                            }

                            var buf_ = new Matrix(new double[] { df, dm });

                            return buf_;
                        }, 0, to, gpt);

                        integral = intgV.Integrate();
                    }

                    var v_i = integral[0, 0];
                    var m_i = integral[1, 0];

                    var memb = buf.MembraneTensor;
                    var bnd = buf.BendingTensor;

                    if (this._direction == BeamDirection.Y)
                    {
                        var v = memb.S12 = memb.S21 = -(v_i + v0);
                        bnd.M13 = bnd.M31 = -(m0 + m_i + (v * to * -1));
                    }
                    else
                    {
                        var v = memb.S13 = memb.S31 = -(v_i + v0);
                        bnd.M12 = bnd.M21 = -(m0 + m_i + (v * to * +1));
                    }

                    buf.MembraneTensor = memb;
                    buf.BendingTensor= bnd;

                    //return buf;
                }
            }



            #endregion

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Displacement GetLoadDisplacementAt(Element targetElement, Load load, double[] isoLocation)
        {
            throw new NotImplementedException();
        }

        
        /// <inheritdoc/>
        public Displacement GetLocalDisplacementAt(Element targetElement, Displacement[] localDisplacements, params double[] isoCoords)
        {
            var nc = targetElement.Nodes.Length;

            var ld = localDisplacements;

            Matrix B = new Matrix(2, 4);

            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var n = GetNMatrixAt(targetElement, isoCoords);

            var d = GetDMatrixAt(targetElement, isoCoords);

            var u = new Matrix(2 * nc, 1);

            var j = GetJMatrixAt(targetElement, isoCoords).Determinant();

            if (_direction == BeamDirection.Z)
                u.FillColumn(0, ld[0].DZ, ld[0].RY, ld[1].DZ, ld[1].RY);
            else
                u.FillColumn(0, ld[0].DY, ld[0].RZ, ld[1].DY, ld[1].RZ);

            var f = n * u;

            var ei = d[0, 0];

            f.MultiplyRowByConstant(1, 1 / j);
            f.MultiplyRowByConstant(2, ei / (j * j));
            f.MultiplyRowByConstant(3, ei / (j * j * j));

            var buf = new Displacement();

            if (_direction == BeamDirection.Y)
            {
                buf.DY = f[0, 0];
                buf.RZ = -f[1, 0];
            }
            else
            {
                buf.DZ = f[0, 0];
                buf.RY = f[1, 0];
            }
            
            return buf;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<DoF, double>> GetLocalInternalForceAt(Element targetElement, Displacement[] localDisplacements, params double[] isoCoords)
        {
            var nc = targetElement.Nodes.Length;

            var ld = localDisplacements;

            Matrix B = new Matrix(2, 4);

            var xi = isoCoords[0];

            if (xi < -1 || xi > 1)
                throw new ArgumentOutOfRangeException(nameof(isoCoords));

            var bar = targetElement as BarElement;

            if (bar == null)
                throw new Exception();

            var n = GetNMatrixAt(targetElement, isoCoords);

            var oldDir = this._direction;

            //TODO: this is very odd and not true.
            //TODO: should not change the _direction.
            this._direction = this._direction == BeamDirection.Y ? BeamDirection.Z : BeamDirection.Y;
            var d = GetDMatrixAt(targetElement, isoCoords);
            this._direction = oldDir;

            var u = new Matrix(2 * nc, 1);

            var j = GetJMatrixAt(targetElement, isoCoords).Determinant();

            if (_direction == BeamDirection.Y)
                u.FillColumn(0, ld[0].DZ, ld[0].RY, ld[1].DZ, ld[1].RY);
            else
                u.FillColumn(0, ld[0].DY, ld[0].RZ, ld[1].DY, ld[1].RZ);

            var ei = d[0, 0];

            n.MultiplyRowByConstant(1, 1 / j);
            n.MultiplyRowByConstant(2, ei / (j * j));
            n.MultiplyRowByConstant(3, ei / (j * j * j));

            var f =  n * u;

            f.MultiplyByConstant(-1);
            
            var buf = new List<Tuple<DoF, double>>();

            if (_direction == BeamDirection.Y)
            {
                buf.Add(Tuple.Create(DoF.Ry, f[2, 0]));
                buf.Add(Tuple.Create(DoF.Dz, f[3, 0]));
            }
            else
            {
                buf.Add(Tuple.Create(DoF.Rz, -f[2, 0]));
                buf.Add(Tuple.Create(DoF.Dy, f[3, 0]));
            }

            return buf;
        }


        public Force[] GetLocalEquivalentNodalLoads(Element targetElement, Load load)
        {
            //https://www.quora.com/How-should-I-perform-element-forces-or-distributed-forces-to-node-forces-translation-in-the-beam-element

            var tr = targetElement.GetTransformationManager();

            #region uniform & trapezoid

            if (load is UniformLoad || load is PartialTrapezoidalLoad)
            {

                Func<double, double> magnitude;
                Vector localDir;

                double xi0, xi1;
                int degree;//polynomial degree of magnitude function

                #region inits
                if (load is UniformLoad)
                {
                    var uld = (load as UniformLoad);

                    magnitude = (xi => uld.Magnitude);
                    localDir = uld.Direction;

                    if (uld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = -1;
                    xi1 = 1;
                    degree = 0;
                }
                else
                {
                    var tld = (load as PartialTrapezoidalLoad);

                    magnitude = (xi => (load as PartialTrapezoidalLoad).GetMagnitudesAt(xi, 0, 0)[0]);
                    localDir = tld.Direction;

                    if (tld.CoordinationSystem == CoordinationSystem.Global)
                        localDir = tr.TransformGlobalToLocal(localDir);

                    xi0 = tld.StarIsoLocations[0];
                    xi1 = tld.EndIsoLocations[0];
                    degree = 1;
                }

                localDir = localDir.GetUnit();
                #endregion

                {

                    var nOrd = GetNMaxOrder(targetElement).Max();

                    var gpt = (nOrd + degree) / 2 + 1;//gauss point count

                    var intg = GaussianIntegrator.CreateFor1DProblem(xi =>
                    {
                        var shp = GetNMatrixAt(targetElement, xi, 0, 0);
                        var q__ = magnitude(xi);
                        var j = GetJMatrixAt(targetElement, xi, 0, 0);
                        shp.MultiplyByConstant(j.Determinant());

                        var q_ = localDir * q__;

                        if (this._direction == BeamDirection.Y)
                            shp.MultiplyByConstant(q_.Z);
                        else
                            shp.MultiplyByConstant(q_.Y);

                        return shp;
                    }, xi0, xi1, gpt);

                    var res = intg.Integrate();

                    var localForces = new Force[2];

                    if (this._direction == BeamDirection.Y)
                    {
                        var fz0 = res[0, 0];
                        var my0 = res[0, 1];
                        var fz1 = res[0, 2];
                        var my1 = res[0, 3];

                        localForces[0] = new Force(0, 0, fz0, 0, my0, 0);
                        localForces[1] = new Force(0, 0, fz1, 0, my1, 0);
                    }
                    else
                    {

                        var fy0 = res[0, 0];
                        var mz0 = res[0, 1];
                        var fy1 = res[0, 2];
                        var mz1 = res[0, 3];

                        localForces[0] = new Force(0, fy0, 0, 0, 0, mz0);
                        localForces[1] = new Force(0, fy1, 0, 0, 0, mz1);
                    }

                    return localForces;
                }
            }
            
            

            #endregion

            

            throw new NotImplementedException();
        }
    }
}

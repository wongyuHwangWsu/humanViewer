// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace humanViewer
{
    using System;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;
    using HelixToolkit.Wpf.SharpDX;
    using SharpDX;
    using Media3D = System.Windows.Media.Media3D;
    using Point3D = System.Windows.Media.Media3D.Point3D;
    using Vector3D = System.Windows.Media.Media3D.Vector3D;
    using Transform3D = System.Windows.Media.Media3D.Transform3D;
    using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
    using Color = System.Windows.Media.Color;
    using Plane = SharpDX.Plane;
    using Vector3 = SharpDX.Vector3;
    using Colors = System.Windows.Media.Colors;
    using Color4 = SharpDX.Color4;
    using HelixToolkit.Wpf;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using SharpDX.Direct3D11;
    using System.Windows;
    using System.Windows.Data;
    using HelixToolkit.Wpf.SharpDX.Extensions;

    public class MainViewModel : BaseViewModel
    {
        public string Name { get; set; }
        public MainViewModel ViewModel { get { return this; } }
        public MeshGeometry3D HeartModel { get; private set; }
       // public MeshGeometry3D Floor { get; private set; }
        public MeshGeometry3D Human { get; private set; }
        public MeshGeometry3D HumanModel { get; private set; }

        //public MeshGeometry3D CarModel { private set; get; }

        public PhongMaterial HumanMaterial { get; set; }
        public PhongMaterial HeartMaterial { get; set; }
        //public PhongMaterial FloorMaterial { get; set; }
        public PhongMaterial LightModelMaterial { get; set; }

        public Transform3D HeartTransform { private set; get; }

        public Vector3D Light1Direction { get; set; }
        public Color Light1Color { get; set; }
        public Color AmbientLightColor { get; set; }
        private Vector3D camLookDir = new Vector3D(0, 0, -1);
        public Vector3D CamLookDir
        {
            set
            {
                if (camLookDir != value)
                {
                    camLookDir = value;
                    OnPropertyChanged();
                    Light1Direction = value;
                }
            }
            get
            {
                return camLookDir;
            }
        }

        public Matrix[] HeartInstances { private set; get; }


        public bool EnablePlane1 { set; get; } = true;
        public Plane Plane1 { set; get; } = new Plane(new Vector3(0, -1, 0), -20);

        public bool EnablePlane2 { set; get; } = false;
        public Plane Plane2 { set; get; } = new Plane(new Vector3(-1, 0, 0), -15);


        public MeshGeometry3D Plane1Model { private set; get; }
        public MeshGeometry3D Plane2Model { private set; get; }
        public TranslateTransform3D Plane1Transform { set; get; }
        public TranslateTransform3D Plane2Transform { set; get; }
        private float plane1OffsetY = -15.0f;
        public float Plane1offsetY
        {
            set
            {
                if(plane1OffsetY != value)
                {
                    plane1OffsetY = value;
                    Plane1Transform.OffsetY = -value;
                    Plane1 = new Plane(new Vector3(0, -1, 0), value);
                    OnPropertyChanged("Plane1");
                }
            }
            get
            {
                return plane1OffsetY;
            }
        }

        public PhongMaterial PlaneMaterial { private set; get; }

        public MainViewModel()
        {
            EffectsManager = new DefaultEffectsManager();
            RenderTechnique = EffectsManager[DefaultRenderTechniqueNames.Blinn];
            // ----------------------------------------------
            // titles
            this.Title = "Lighting Demo";
            this.SubTitle = "WPF & SharpDX";

            // ----------------------------------------------
            // camera setup
            this.Camera = new PerspectiveCamera { Position = new Point3D(0, 0, 0),  UpDirection = new Vector3D(0, 1, 0) };

            camLookDir = new Vector3D(0, 0, -1);
            //Camera.ZoomExtents()
            // ----------------------------------------------
            // setup scene
            this.AmbientLightColor = Colors.DimGray;
            this.Light1Color = Colors.LightGray;


            this.Light1Direction = new Vector3D(-100, -100, -100);
            SetupCameraBindings(Camera);

            // ----------------------------------------------
            // ----------------------------------------------
            // scene model3d
            this.HeartMaterial = PhongMaterials.Silver;
            this.HeartMaterial.DiffuseMap = LoadFileToMemory(new System.Uri(@"Heart_CM_TEXTURE.jpg", System.UriKind.RelativeOrAbsolute).ToString());
            this.HeartMaterial.NormalMap = LoadFileToMemory(new System.Uri(@"Heart_NM_TEXTURE.jpg", System.UriKind.RelativeOrAbsolute).ToString());

            this.HumanMaterial = PhongMaterials.Bisque;
            this.HumanMaterial.DiffuseMap = LoadFileToMemory(new System.Uri(@"RUST_3d_Low1_Difuse.jpg", System.UriKind.RelativeOrAbsolute).ToString());
            this.HumanMaterial.NormalMap = LoadFileToMemory(new System.Uri(@"RUST_3d_Low1_Normal.jpg", System.UriKind.RelativeOrAbsolute).ToString());


            var scale = new Vector3(1f);

            var humanitems = Load3ds("RUST_3d_Low1.obj").Select(x => x.Geometry as MeshGeometry3D).ToArray();

            foreach (var item in humanitems)
            {
                for (int i = 0; i < item.Positions.Count; ++i)
                {
                    item.Positions[i] = item.Positions[i] * scale;
                }

            }
            HumanModel = MeshGeometry3D.Merge(humanitems);
            

            var caritems = Load3ds("Heart.obj").Select(x => x.Geometry as MeshGeometry3D).ToArray();

            var heartScale = new Vector3(70f);
            foreach (var item in caritems)
            {
                for (int i = 0; i < item.Positions.Count; ++i)
                {
                    item.Positions[i] = item.Positions[i] * heartScale;
                }
            }
            HeartModel = MeshGeometry3D.Merge(caritems);
            
            HeartInstances = new Matrix[1];
            HeartInstances[0] = Matrix.Translation(new Vector3(10,125,3));

            var builder = new MeshBuilder(true, false, false);
            builder.AddBox(new Vector3(), 200, 0.5, 200);
            Plane1Model = builder.ToMeshGeometry3D();

            builder = new MeshBuilder(true, false, false);
            builder.AddBox(new Vector3(), 0.1, 40, 40);
            Plane2Model = builder.ToMeshGeometry3D();

            PlaneMaterial = new PhongMaterial() { DiffuseColor = new Color4(0.1f, 0.8f, 0.1f, 0.5f) };

            Plane1Transform = new TranslateTransform3D(new Vector3D(0, 15, 0));
        }

        public List<Object3D> Load3ds(string path)
        {
            var reader = new ObjReader();
            var list = reader.Read(path);
            return list;
        }

        public void SetupCameraBindings(Camera camera)
        {
            if (camera is ProjectionCamera)
            {
                SetBinding("CamLookDir", camera, ProjectionCamera.LookDirectionProperty, this);
            }
        }

        private static void SetBinding(string path, DependencyObject dobj, DependencyProperty property, object viewModel, BindingMode mode = BindingMode.TwoWay)
        {
            var binding = new Binding(path);
            binding.Source = viewModel;
            binding.Mode = mode;
            BindingOperations.SetBinding(dobj, property, binding);
        }
    }
}
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace XJProcess
{
    public class View3DService
    {
        public Model3DGroup Load3DModel(string objFilePath, Transform3D rotationTransform)
        {
            if (!File.Exists(objFilePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file 3D tại: {objFilePath}");
            }
            string modelDirPath = Path.GetDirectoryName(objFilePath);
            var reader = new ObjReader
            {
                TexturePath = modelDirPath
            };
            Model3DGroup loadedModel = reader.Read(objFilePath);
            if (loadedModel == null || loadedModel.Children.Count == 0)
            {
                throw new Exception("File .obj không chứa dữ liệu 3D hợp lệ!");
            }
            ApplyWoodMaterial(loadedModel);
            ApplyCenteringAndScaling(loadedModel, rotationTransform);
            return loadedModel;
        }

        /// <summary>
        /// Tạo và ép Material màu gỗ ấm áp cho toàn bộ mô hình
        /// </summary>
        private void ApplyWoodMaterial(Model3DGroup group)
        {
            if (group == null) return;
            MaterialGroup grayWoodMaterial = new MaterialGroup();
            grayWoodMaterial.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(108, 93, 83))));
            grayWoodMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(140, 130, 120)), 4));
            foreach (var child in group.Children)
            {
                if (child is GeometryModel3D geomModel)
                {
                    geomModel.Material = grayWoodMaterial;
                    geomModel.BackMaterial = grayWoodMaterial;
                }
                else if (child is Model3DGroup childGroup)
                {
                    ApplyWoodMaterial(childGroup);
                }
            }
        }

        private void ApplyCenteringAndScaling(Model3DGroup model, Transform3D rotationTransform)
        {
            Rect3D bounds = model.Bounds;
            double centerX = bounds.X + bounds.SizeX / 2.0;
            double centerY = bounds.Y + bounds.SizeY / 2.0;
            double centerZ = bounds.Z + bounds.SizeZ / 2.0;
            double offsetY = 0.1;
            double offsetZ = 0.0;
            double maxDimension = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));
            double scale = maxDimension > 0 ? (2.0 / maxDimension) : 1.0;
            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D(-centerX, -(centerY + offsetY), -(centerZ + offsetZ)));
            if (rotationTransform != null)
            {
                transformGroup.Children.Add(rotationTransform);
            }
            transformGroup.Children.Add(new ScaleTransform3D(scale, scale, scale));
            model.Transform = transformGroup;
        }

        public OrthographicCamera GetFrontFlatCamera()
        {
            return new OrthographicCamera
            {
                Position = new Point3D(0, 0, 10),
                LookDirection = new Vector3D(0, 0, -10),
                UpDirection = new Vector3D(0, 1, 0),
                Width = 2.8
            };
        }
    }
}
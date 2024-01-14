/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated September 24, 2021. Replaces all prior versions.
 *
 * Copyright (c) 2013-2021, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

#if (UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_WSA || UNITY_WP8 || UNITY_WP8_1)
#define IS_UNITY
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

#if WINDOWS_STOREAPP
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace Spine {
	public class SkeletonBinary : SkeletonLoader {
		public const int BONE_ROTATE = 0;
		public const int BONE_TRANSLATE = 1;
		public const int BONE_TRANSLATEX = 2;
		public const int BONE_TRANSLATEY = 3;
		public const int BONE_SCALE = 4;
		public const int BONE_SCALEX = 5;
		public const int BONE_SCALEY = 6;
		public const int BONE_SHEAR = 7;
		public const int BONE_SHEARX = 8;
		public const int BONE_SHEARY = 9;

		public const int SLOT_ATTACHMENT = 0;
		public const int SLOT_RGBA = 1;
		public const int SLOT_RGB = 2;
		public const int SLOT_RGBA2 = 3;
		public const int SLOT_RGB2 = 4;
		public const int SLOT_ALPHA = 5;

		public const int ATTACHMENT_DEFORM = 0;
		public const int ATTACHMENT_SEQUENCE = 1;

		public const int PATH_POSITION = 0;
		public const int PATH_SPACING = 1;
		public const int PATH_MIX = 2;

		public const int CURVE_LINEAR = 0;
		public const int CURVE_STEPPED = 1;
		public const int CURVE_BEZIER = 2;

		public bool isOldAnimCurveSpine = false;

		public int actualVersionID = -1;

		public SkeletonBinary (AttachmentLoader attachmentLoader)
			: base(attachmentLoader) {
		}

		public SkeletonBinary (params Atlas[] atlasArray)
			: base(atlasArray) {
		}

#if !ISUNITY && WINDOWS_STOREAPP
		private async Task<SkeletonData> ReadFile(string path) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			using (BufferedStream input = new BufferedStream(await folder.GetFileAsync(path).AsTask().ConfigureAwait(false))) {
				SkeletonData skeletonData = ReadSkeletonData(input);
				skeletonData.Name = Path.GetFileNameWithoutExtension(path);
				return skeletonData;
			}
		}

		public override SkeletonData ReadSkeletonData (string path) {
			return this.ReadFile(path).Result;
		}
#else
		public override SkeletonData ReadSkeletonData (string path) {
#if WINDOWS_PHONE
			using (BufferedStream input = new BufferedStream(Microsoft.Xna.Framework.TitleContainer.OpenStream(path))) {
#else
			using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
#endif
				SkeletonData skeletonData = ReadSkeletonData(input);
				skeletonData.name = Path.GetFileNameWithoutExtension(path);
				return skeletonData;
			}
		}
#endif // WINDOWS_STOREAPP

		public static readonly TransformMode[] TransformModeValues = {
			TransformMode.Normal,
			TransformMode.OnlyTranslation,
			TransformMode.NoRotationOrReflection,
			TransformMode.NoScale,
			TransformMode.NoScaleOrReflection
		};

		/// <summary>Returns the version string of binary skeleton data.</summary>
		public static string GetVersionString (Stream file) {
			if (file == null) throw new ArgumentNullException("file");

			SkeletonInput input = new SkeletonInput(file);
			return input.GetVersionString();
		}

		public SkeletonData ReadSkeletonData (Stream file) {

			long startPosition = file.Position;

			if (file == null) throw new ArgumentNullException("file");
			float scale = this.scale;
			
			SkeletonData skeletonData = new SkeletonData();
			SkeletonInput input = new SkeletonInput(file);

			long hash = input.ReadLong();
			skeletonData.hash = hash == 0 ? null : hash.ToString();
			skeletonData.version = input.ReadString();

			actualVersionID = GetVersionID(skeletonData.version);

			isOldAnimCurveSpine = skeletonData.version.Length > 13 && actualVersionID < 40;

			if (skeletonData.version.Length == 0) skeletonData.version = null;
			// early return for old 3.8 format instead of reading past the end
			if (!isOldAnimCurveSpine && skeletonData.version.Length > 13) return null;

            if (isOldAnimCurveSpine)
            {
				// Roll Back And Read Again
				file.Position = startPosition;
				skeletonData.hash = input.ReadString();
				skeletonData.version = input.ReadString();
			}

			if(actualVersionID > 37)
            {
				skeletonData.x = input.ReadFloat();
				skeletonData.y = input.ReadFloat();
			}
			
			skeletonData.width = input.ReadFloat();
			skeletonData.height = input.ReadFloat();

			bool nonessential = input.ReadBoolean();

			if (nonessential) {
				skeletonData.fps = input.ReadFloat();

				skeletonData.imagesPath = input.ReadString();
				if (string.IsNullOrEmpty(skeletonData.imagesPath)) skeletonData.imagesPath = null;

				if (actualVersionID > 36)
                {
					skeletonData.audioPath = input.ReadString();
					if (string.IsNullOrEmpty(skeletonData.audioPath)) skeletonData.audioPath = null;
				}
			}

			int n;
			Object[] o;

			if(actualVersionID > 37)
            {
				// Strings.
				o = input.strings = new String[n = input.ReadInt(true)];
				for (int i = 0; i < n; i++)
					o[i] = input.ReadString();
            }
            else
            {
				input.DisableStringRef();
			}
			
			// Bones.
			BoneData[] bones = skeletonData.bones.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0; i < n; i++) {
				String name = input.ReadString();
				BoneData parent = i == 0 ? null : bones[input.ReadInt(true)];
				BoneData data = new BoneData(i, name, parent);
				data.rotation = input.ReadFloat();
				data.x = input.ReadFloat() * scale;
				data.y = input.ReadFloat() * scale;
				data.scaleX = input.ReadFloat();
				data.scaleY = input.ReadFloat();
				data.shearX = input.ReadFloat();
				data.shearY = input.ReadFloat();
				data.Length = input.ReadFloat() * scale;
				data.transformMode = TransformModeValues[input.ReadInt(true)];
				if (actualVersionID > 37)
                {
					data.skinRequired = input.ReadBoolean();
				}

				if (nonessential) input.ReadInt(); // Skip bone color.
				bones[i] = data;
			}

			// Slots.
			SlotData[] slots = skeletonData.slots.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0; i < n; i++) {
				String slotName = input.ReadString();
				BoneData boneData = bones[input.ReadInt(true)];
				SlotData slotData = new SlotData(i, slotName, boneData);
				int color = input.ReadInt();
				slotData.r = ((color & 0xff000000) >> 24) / 255f;
				slotData.g = ((color & 0x00ff0000) >> 16) / 255f;
				slotData.b = ((color & 0x0000ff00) >> 8) / 255f;
				slotData.a = ((color & 0x000000ff)) / 255f;

				if(actualVersionID > 35)
                {
					int darkColor = input.ReadInt(); // 0x00rrggbb
					if (darkColor != -1)
					{
						slotData.hasSecondColor = true;
						slotData.r2 = ((darkColor & 0x00ff0000) >> 16) / 255f;
						slotData.g2 = ((darkColor & 0x0000ff00) >> 8) / 255f;
						slotData.b2 = ((darkColor & 0x000000ff)) / 255f;
					}
				}

				slotData.attachmentName = input.ReadStringRef();
				slotData.blendMode = (BlendMode)input.ReadInt(true);
				slots[i] = slotData;
			}

			// IK constraints.
			o = skeletonData.ikConstraints.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0, nn; i < n; i++) {
				IkConstraintData data = new IkConstraintData(input.ReadString());
				data.order = input.ReadInt(true);
				if (actualVersionID > 37)
                {
					data.skinRequired = input.ReadBoolean();
				}
				BoneData[] constraintBones = data.bones.Resize(nn = input.ReadInt(true)).Items;
				for (int ii = 0; ii < nn; ii++)
					constraintBones[ii] = bones[input.ReadInt(true)];
				data.target = bones[input.ReadInt(true)];
				data.mix = input.ReadFloat();
				if (actualVersionID > 37)
				{
					data.softness = input.ReadFloat() * scale;
				}
				data.bendDirection = input.ReadSByte();
				if (actualVersionID > 36)
				{
					data.compress = input.ReadBoolean();
					data.stretch = input.ReadBoolean();
					data.uniform = input.ReadBoolean();
				}

				o[i] = data;
			}

			// Transform constraints.
			o = skeletonData.transformConstraints.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0, nn; i < n; i++) {
				TransformConstraintData data = new TransformConstraintData(input.ReadString());
				data.order = input.ReadInt(true);
				if (actualVersionID > 37)
                {
					data.skinRequired = input.ReadBoolean();
				}
				BoneData[] constraintBones = data.bones.Resize(nn = input.ReadInt(true)).Items;
				for (int ii = 0; ii < nn; ii++)
					constraintBones[ii] = bones[input.ReadInt(true)];
				data.target = bones[input.ReadInt(true)];
                if (actualVersionID > 35)
                {
					data.local = input.ReadBoolean();
					data.relative = input.ReadBoolean();
				}
				data.offsetRotation = input.ReadFloat();
				data.offsetX = input.ReadFloat() * scale;
				data.offsetY = input.ReadFloat() * scale;
				data.offsetScaleX = input.ReadFloat();
				data.offsetScaleY = input.ReadFloat();
				data.offsetShearY = input.ReadFloat();
				data.mixRotate = input.ReadFloat();
				data.mixX = input.ReadFloat();
				data.mixY = isOldAnimCurveSpine ? data.mixX : input.ReadFloat();
				data.mixScaleX = input.ReadFloat();
				data.mixScaleY = isOldAnimCurveSpine ? data.mixScaleX : input.ReadFloat();
				data.mixShearY = input.ReadFloat();
				o[i] = data;
			}

			// Path constraints
			o = skeletonData.pathConstraints.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0, nn; i < n; i++) {
				PathConstraintData data = new PathConstraintData(input.ReadString());
				data.order = input.ReadInt(true);
				if (actualVersionID > 37)
                {
					data.skinRequired = input.ReadBoolean();
				}
				Object[] constraintBones = data.bones.Resize(nn = input.ReadInt(true)).Items;
				for (int ii = 0; ii < nn; ii++)
					constraintBones[ii] = bones[input.ReadInt(true)];
				data.target = slots[input.ReadInt(true)];
				data.positionMode = (PositionMode)Enum.GetValues(typeof(PositionMode)).GetValue(input.ReadInt(true));
				data.spacingMode = (SpacingMode)Enum.GetValues(typeof(SpacingMode)).GetValue(input.ReadInt(true));
				data.rotateMode = (RotateMode)Enum.GetValues(typeof(RotateMode)).GetValue(input.ReadInt(true));
				data.offsetRotation = input.ReadFloat();
				data.position = input.ReadFloat();
				if (data.positionMode == PositionMode.Fixed) data.position *= scale;
				data.spacing = input.ReadFloat();
				if (data.spacingMode == SpacingMode.Length || data.spacingMode == SpacingMode.Fixed) data.spacing *= scale;
				data.mixRotate = input.ReadFloat();
				data.mixX = input.ReadFloat();
				data.mixY = isOldAnimCurveSpine ? data.mixX : input.ReadFloat();
				o[i] = data;
			}

			// Default skin.
			Skin defaultSkin = ReadSkin(input, skeletonData, true, nonessential);
			if (defaultSkin != null) {
				skeletonData.defaultSkin = defaultSkin;
				skeletonData.skins.Add(defaultSkin);
			}

			// Skins.
			{
				int i = skeletonData.skins.Count;
				o = skeletonData.skins.Resize(n = i + input.ReadInt(true)).Items;
				for (; i < n; i++)
					o[i] = ReadSkin(input, skeletonData, actualVersionID < 38, nonessential);
			}

			// Linked meshes.
			n = linkedMeshes.Count;
			for (int i = 0; i < n; i++) {
				LinkedMesh linkedMesh = linkedMeshes[i];
				Skin skin = linkedMesh.skin == null ? skeletonData.DefaultSkin : skeletonData.FindSkin(linkedMesh.skin);
				if (skin == null) throw new Exception("Skin not found: " + linkedMesh.skin);
				Attachment parent = skin.GetAttachment(linkedMesh.slotIndex, linkedMesh.parent);
				if (parent == null) throw new Exception("Parent mesh not found: " + linkedMesh.parent);
				linkedMesh.mesh.TimelineAttachment = linkedMesh.inheritTimelines ? (VertexAttachment)parent : linkedMesh.mesh;
				linkedMesh.mesh.ParentMesh = (MeshAttachment)parent;
				if (linkedMesh.mesh.Sequence == null) linkedMesh.mesh.UpdateRegion();
			}
			linkedMeshes.Clear();

			// Events.
			o = skeletonData.events.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0; i < n; i++) {
				EventData data = new EventData(input.ReadStringRef());
				data.Int = input.ReadInt(false);
				data.Float = input.ReadFloat();
				data.String = input.ReadString();

				if(actualVersionID > 36)
                {
					data.AudioPath = input.ReadString();
					if (data.AudioPath != null)
					{
						data.Volume = input.ReadFloat();
						data.Balance = input.ReadFloat();
					}
				}
				
				o[i] = data;
			}

			// Animations.
			o = skeletonData.animations.Resize(n = input.ReadInt(true)).Items;
			for (int i = 0; i < n; i++)
				o[i] = ReadAnimation(input.ReadString(), input, skeletonData, isOldAnimCurveSpine);

			return skeletonData;
		}

		/// <returns>May be null.</returns>
		private Skin ReadSkin (SkeletonInput input, SkeletonData skeletonData, bool defaultSkin, bool nonessential) {

			Skin skin;
			int slotCount;

			if (defaultSkin) {
				slotCount = input.ReadInt(true);
				if (slotCount == 0) return null;
				skin = new Skin("default");
			} else {
				skin = new Skin(input.ReadStringRef());
				Object[] bones = skin.bones.Resize(input.ReadInt(true)).Items;
				BoneData[] bonesItems = skeletonData.bones.Items;
				for (int i = 0, n = skin.bones.Count; i < n; i++)
					bones[i] = bonesItems[input.ReadInt(true)];

				IkConstraintData[] ikConstraintsItems = skeletonData.ikConstraints.Items;
				for (int i = 0, n = input.ReadInt(true); i < n; i++)
					skin.constraints.Add(ikConstraintsItems[input.ReadInt(true)]);
				TransformConstraintData[] transformConstraintsItems = skeletonData.transformConstraints.Items;
				for (int i = 0, n = input.ReadInt(true); i < n; i++)
					skin.constraints.Add(transformConstraintsItems[input.ReadInt(true)]);
				PathConstraintData[] pathConstraintsItems = skeletonData.pathConstraints.Items;
				for (int i = 0, n = input.ReadInt(true); i < n; i++)
					skin.constraints.Add(pathConstraintsItems[input.ReadInt(true)]);
				skin.constraints.TrimExcess();

				slotCount = input.ReadInt(true);
			}

			for (int i = 0; i < slotCount; i++) {
				int slotIndex = input.ReadInt(true);
				for (int ii = 0, nn = input.ReadInt(true); ii < nn; ii++) {
					String name = input.ReadStringRef();
					Attachment attachment = ReadAttachment(input, skeletonData, skin, slotIndex, name, nonessential,isOldAnimCurveSpine);
					if (attachment != null) skin.SetAttachment(slotIndex, name, attachment);
				}
			}
			return skin;
		}

		private Attachment ReadAttachment (SkeletonInput input, SkeletonData skeletonData, Skin skin, int slotIndex,
			String attachmentName, bool nonessential,bool isOldSpine) {
			float scale = this.scale;

			String name = input.ReadStringRef();
			if (name == null) name = attachmentName;

			switch ((AttachmentType)input.ReadByte()) {
			case AttachmentType.Region: {
				String path = input.ReadStringRef();
				float rotation = input.ReadFloat();
				float x = input.ReadFloat();
				float y = input.ReadFloat();
				float scaleX = input.ReadFloat();
				float scaleY = input.ReadFloat();
				float width = input.ReadFloat();
				float height = input.ReadFloat();
				int color = input.ReadInt();
				Sequence sequence = isOldSpine ? null : ReadSequence(input);

				if (path == null) path = name;
				RegionAttachment region = attachmentLoader.NewRegionAttachment(skin, name, path, sequence);
				if (region == null) return null;
				region.Path = path;
				region.x = x * scale;
				region.y = y * scale;
				region.scaleX = scaleX;
				region.scaleY = scaleY;
				region.rotation = rotation;
				region.width = width * scale;
				region.height = height * scale;
				region.r = ((color & 0xff000000) >> 24) / 255f;
				region.g = ((color & 0x00ff0000) >> 16) / 255f;
				region.b = ((color & 0x0000ff00) >> 8) / 255f;
				region.a = ((color & 0x000000ff)) / 255f;
				region.sequence = sequence;
				if (sequence == null) region.UpdateRegion();
				return region;
			}
			case AttachmentType.Boundingbox: {
				int vertexCount = input.ReadInt(true);
				Vertices vertices = ReadVertices(input, vertexCount);
				if (nonessential) input.ReadInt(); //int color = nonessential ? input.ReadInt() : 0; // Avoid unused local warning.

				BoundingBoxAttachment box = attachmentLoader.NewBoundingBoxAttachment(skin, name);
				if (box == null) return null;
				box.worldVerticesLength = vertexCount << 1;
				box.vertices = vertices.vertices;
				box.bones = vertices.bones;
				// skipped porting: if (nonessential) Color.rgba8888ToColor(box.getColor(), color);
				return box;
			}
			case AttachmentType.Mesh: {
				String path = input.ReadStringRef();
				int color = input.ReadInt();
				int vertexCount = input.ReadInt(true);
				float[] uvs = ReadFloatArray(input, vertexCount << 1, 1);
				int[] triangles = ReadShortArray(input);
				Vertices vertices = ReadVertices(input, vertexCount);
				int hullLength = input.ReadInt(true);
				Sequence sequence = isOldSpine ? null : ReadSequence(input);
				int[] edges = null;
				float width = 0, height = 0;
				if (nonessential) {
					edges = ReadShortArray(input);
					width = input.ReadFloat();
					height = input.ReadFloat();
				}

				if (path == null) path = name;
				MeshAttachment mesh = attachmentLoader.NewMeshAttachment(skin, name, path, sequence);
				if (mesh == null) return null;
				mesh.Path = path;
				mesh.r = ((color & 0xff000000) >> 24) / 255f;
				mesh.g = ((color & 0x00ff0000) >> 16) / 255f;
				mesh.b = ((color & 0x0000ff00) >> 8) / 255f;
				mesh.a = ((color & 0x000000ff)) / 255f;
				mesh.bones = vertices.bones;
				mesh.vertices = vertices.vertices;
				mesh.WorldVerticesLength = vertexCount << 1;
				mesh.triangles = triangles;
				mesh.regionUVs = uvs;
				if (sequence == null) mesh.UpdateRegion();
				mesh.HullLength = hullLength << 1;
				mesh.Sequence = sequence;
				if (nonessential) {
					mesh.Edges = edges;
					mesh.Width = width * scale;
					mesh.Height = height * scale;
				}
				return mesh;
			}
			case AttachmentType.Linkedmesh: {
				String path = input.ReadStringRef();
				int color = input.ReadInt();
				String skinName = input.ReadStringRef();
				String parent = input.ReadStringRef();
				bool inheritTimelines = input.ReadBoolean();
				Sequence sequence = isOldSpine ? null : ReadSequence(input);
				float width = 0, height = 0;
				if (nonessential) {
					width = input.ReadFloat();
					height = input.ReadFloat();
				}

				if (path == null) path = name;
				MeshAttachment mesh = attachmentLoader.NewMeshAttachment(skin, name, path, sequence);
				if (mesh == null) return null;
				mesh.Path = path;
				mesh.r = ((color & 0xff000000) >> 24) / 255f;
				mesh.g = ((color & 0x00ff0000) >> 16) / 255f;
				mesh.b = ((color & 0x0000ff00) >> 8) / 255f;
				mesh.a = ((color & 0x000000ff)) / 255f;
				mesh.Sequence = sequence;
				if (nonessential) {
					mesh.Width = width * scale;
					mesh.Height = height * scale;
				}
				linkedMeshes.Add(new SkeletonJson.LinkedMesh(mesh, skinName, slotIndex, parent, inheritTimelines));
				return mesh;
			}
			case AttachmentType.Path: {
				bool closed = input.ReadBoolean();
				bool constantSpeed = input.ReadBoolean();
				int vertexCount = input.ReadInt(true);
				Vertices vertices = ReadVertices(input, vertexCount);
				float[] lengths = new float[vertexCount / 3];
				for (int i = 0, n = lengths.Length; i < n; i++)
					lengths[i] = input.ReadFloat() * scale;
				if (nonessential) input.ReadInt(); //int color = nonessential ? input.ReadInt() : 0;

				PathAttachment path = attachmentLoader.NewPathAttachment(skin, name);
				if (path == null) return null;
				path.closed = closed;
				path.constantSpeed = constantSpeed;
				path.worldVerticesLength = vertexCount << 1;
				path.vertices = vertices.vertices;
				path.bones = vertices.bones;
				path.lengths = lengths;
				// skipped porting: if (nonessential) Color.rgba8888ToColor(path.getColor(), color);
				return path;
			}
			case AttachmentType.Point: {
				float rotation = input.ReadFloat();
				float x = input.ReadFloat();
				float y = input.ReadFloat();
				if (nonessential) input.ReadInt(); //int color = nonessential ? input.ReadInt() : 0;

				PointAttachment point = attachmentLoader.NewPointAttachment(skin, name);
				if (point == null) return null;
				point.x = x * scale;
				point.y = y * scale;
				point.rotation = rotation;
				// skipped porting: if (nonessential) point.color = color;
				return point;
			}
			case AttachmentType.Clipping: {
				int endSlotIndex = input.ReadInt(true);
				int vertexCount = input.ReadInt(true);
				Vertices vertices = ReadVertices(input, vertexCount);
				if (nonessential) input.ReadInt();

				ClippingAttachment clip = attachmentLoader.NewClippingAttachment(skin, name);
				if (clip == null) return null;
				clip.EndSlot = skeletonData.slots.Items[endSlotIndex];
				clip.worldVerticesLength = vertexCount << 1;
				clip.vertices = vertices.vertices;
				clip.bones = vertices.bones;
				// skipped porting: if (nonessential) Color.rgba8888ToColor(clip.getColor(), color);
				return clip;
			}
			}
			return null;
		}

		private Sequence ReadSequence (SkeletonInput input) {
			if (!input.ReadBoolean()) return null;
			Sequence sequence = new Sequence(input.ReadInt(true));
			sequence.Start = input.ReadInt(true);
			sequence.Digits = input.ReadInt(true);
			sequence.SetupIndex = input.ReadInt(true);
			return sequence;
		}

		private Vertices ReadVertices (SkeletonInput input, int vertexCount) {
			float scale = this.scale;
			int verticesLength = vertexCount << 1;
			Vertices vertices = new Vertices();
			if (!input.ReadBoolean()) {
				vertices.vertices = ReadFloatArray(input, verticesLength, scale);
				return vertices;
			}
			ExposedList<float> weights = new ExposedList<float>(verticesLength * 3 * 3);
			ExposedList<int> bonesArray = new ExposedList<int>(verticesLength * 3);
			for (int i = 0; i < vertexCount; i++) {
				int boneCount = input.ReadInt(true);
				bonesArray.Add(boneCount);
				for (int ii = 0; ii < boneCount; ii++) {
					bonesArray.Add(input.ReadInt(true));
					weights.Add(input.ReadFloat() * scale);
					weights.Add(input.ReadFloat() * scale);
					weights.Add(input.ReadFloat());
				}
			}

			vertices.vertices = weights.ToArray();
			vertices.bones = bonesArray.ToArray();
			return vertices;
		}

		private float[] ReadFloatArray (SkeletonInput input, int n, float scale) {
			float[] array = new float[n];
			if (scale == 1) {
				for (int i = 0; i < n; i++)
					array[i] = input.ReadFloat();
			} else {
				for (int i = 0; i < n; i++)
					array[i] = input.ReadFloat() * scale;
			}
			return array;
		}

		private int[] ReadShortArray (SkeletonInput input) {
			int n = input.ReadInt(true);
			int[] array = new int[n];
			for (int i = 0; i < n; i++)
				array[i] = (input.ReadByte() << 8) | input.ReadByte();
			return array;
		}
		
		// (Experiment) Special Case For Rotation Reading
		bool limitRotation = false;

		/// <exception cref="SerializationException">SerializationException will be thrown when a Vertex attachment is not found.</exception>
		/// <exception cref="IOException">Throws IOException when a read operation fails.</exception>
		private Animation ReadAnimation (String name, SkeletonInput input, SkeletonData skeletonData,bool isOldCurveSpine) {
			ExposedList<Timeline> timelines = isOldCurveSpine ? actualVersionID == 38 ? new ExposedList<Timeline>(32) : new ExposedList<Timeline>(): new ExposedList<Timeline>(input.ReadInt(true));
			float scale = this.scale;

			// Slot timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				int slotIndex = input.ReadInt(true);
				for (int ii = 0, nn = input.ReadInt(true); ii < nn; ii++) {
					int timelineType = input.ReadByte(), frameCount = input.ReadInt(true), frameLast = frameCount - 1;

					//Old Spine Reflect
					if (isOldCurveSpine)
					{
						switch (timelineType)
						{
							case 2:
								timelineType = 4;
								break;
						}
					}

					switch (timelineType) {
						case SLOT_ATTACHMENT: {
							AttachmentTimeline timeline = new AttachmentTimeline(frameCount, slotIndex);
							for (int frame = 0; frame < frameCount; frame++)
								timeline.SetFrame(frame, input.ReadFloat(), input.ReadStringRef());
							timelines.Add(timeline);
							break;
						}
						case SLOT_RGBA: {
							int bezierCountRGBA = isOldCurveSpine ? frameCount << 2 : input.ReadInt(true);
							RGBATimeline timeline = new RGBATimeline(frameCount, bezierCountRGBA, slotIndex);
							float time = input.ReadFloat();
							float r = input.Read() / 255f, g = input.Read() / 255f;
							float b = input.Read() / 255f, a = input.Read() / 255f;
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, a);
								if (frame == frameLast) break;
								float time2 = 0;
								float r2 = 0, g2 = 0;
								float b2 = 0, a2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								r2 = input.Read() / 255f;
								g2 = input.Read() / 255f;
								b2 = input.Read() / 255f;
								a2 = input.Read() / 255f;
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
									float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
									float cx7 = 0, cy7 = 0, cx8 = 0, cy8 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = r + (r2 - r) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = r + (r2 - r) * param4;

										cx3 = time + (time2 - time) * param1;
										cy3 = g + (g2 - g) * param2;
										cx4 = time + (time2 - time) * param3;
										cy4 = g + (g2 - g) * param4;

										cx5 = time + (time2 - time) * param1;
										cy5 = b + (b2 - b) * param2;
										cx6 = time + (time2 - time) * param3;
										cy6 = b + (b2 - b) * param4;

										cx7 = time + (time2 - time) * param1;
										cy7 = a + (a2 - a) * param2;
										cx8 = time + (time2 - time) * param3;
										cy8 = a + (a2 - a) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, r, r2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									SetBezier(input, timeline, bezier++, frame, 1, time, time2, g, g2, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
									SetBezier(input, timeline, bezier++, frame, 2, time, time2, b, b2, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
									SetBezier(input, timeline, bezier++, frame, 3, time, time2, a, a2, 1, isOldCurveSpine, cx7, cy7, cx8, cy8);
									break;
								}
								time = time2;
								r = r2;
								g = g2;
								b = b2;
								a = a2;
							}
							timelines.Add(timeline);
							break;
						}
						case SLOT_RGB: {
							int bezierCountRGB = isOldCurveSpine ? frameCount * 3 : input.ReadInt(true);
							RGBTimeline timeline = new RGBTimeline(frameCount, bezierCountRGB, slotIndex);
							float time = input.ReadFloat();
							float r = input.Read() / 255f, g = input.Read() / 255f, b = input.Read() / 255f;
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b);
								if (frame == frameLast) break;
								float time2 = 0;
								float r2 = 0, g2 = 0, b2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								r2 = input.Read() / 255f;
								g2 = input.Read() / 255f;
								b2 = input.Read() / 255f;
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
									float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = r + (r2 - r) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = r + (r2 - r) * param4;

										cx3 = time + (time2 - time) * param1;
										cy3 = g + (g2 - g) * param2;
										cx4 = time + (time2 - time) * param3;
										cy4 = g + (g2 - g) * param4;

										cx5 = time + (time2 - time) * param1;
										cy5 = b + (b2 - b) * param2;
										cx6 = time + (time2 - time) * param3;
										cy6 = b + (b2 - b) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, r, r2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									SetBezier(input, timeline, bezier++, frame, 1, time, time2, g, g2, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
									SetBezier(input, timeline, bezier++, frame, 2, time, time2, b, b2, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
									break;
								}
								time = time2;
								r = r2;
								g = g2;
								b = b2;
							}
							timelines.Add(timeline);
							break;
						}
						case SLOT_RGBA2: {
							int bezierCountRGBA2 = isOldCurveSpine ? frameCount * 7 : input.ReadInt(true);
							RGBA2Timeline timeline = new RGBA2Timeline(frameCount, bezierCountRGBA2, slotIndex);
							float time = input.ReadFloat();
							float r = input.Read() / 255f, g = input.Read() / 255f;
							float b = input.Read() / 255f, a = input.Read() / 255f;
							float r2 = input.Read() / 255f, g2 = input.Read() / 255f, b2 = input.Read() / 255f;
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, a, r2, g2, b2);
								if (frame == frameLast) break;
								float time2 = 0;
								float nr = 0, ng = 0;
								float nb = 0, na = 0;
								float nr2 = 0, ng2 = 0, nb2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								nr = input.Read() / 255f;
								ng = input.Read() / 255f;
								nb = input.Read() / 255f;
								na = input.Read() / 255f;
								nr2 = input.Read() / 255f;
								ng2 = input.Read() / 255f;
								nb2 = input.Read() / 255f;
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
									float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
									float cx7 = 0, cy7 = 0, cx8 = 0, cy8 = 0;
									float cx9 = 0, cy9 = 0, cx10 = 0, cy10 = 0;
									float cx11 = 0, cy11 = 0, cx12 = 0, cy12 = 0;
									float cx13 = 0, cy13 = 0, cx14 = 0, cy14 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = r + (nr - r) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = r + (nr - r) * param4;

										cx3 = time + (time2 - time) * param1;
										cy3 = g + (ng - g) * param2;
										cx4 = time + (time2 - time) * param3;
										cy4 = g + (ng - g) * param4;

										cx5 = time + (time2 - time) * param1;
										cy5 = b + (nb - b) * param2;
										cx6 = time + (time2 - time) * param3;
										cy6 = b + (nb - b) * param4;

										cx7 = time + (time2 - time) * param1;
										cy7 = a + (na - a) * param2;
										cx8 = time + (time2 - time) * param3;
										cy8 = a + (na - a) * param4;

										cx9 = time + (time2 - time) * param1;
										cy9 = r2 + (nr2 - r2) * param2;
										cx10 = time + (time2 - time) * param3;
										cy10 = r2 + (nr2 - r2) * param4;

										cx11 = time + (time2 - time) * param1;
										cy11 = g2 + (ng2 - g2) * param2;
										cx12 = time + (time2 - time) * param3;
										cy12 = g2 + (ng2 - g2) * param4;

										cx13 = time + (time2 - time) * param1;
										cy13 = b2 + (nb2 - b2) * param2;
										cx14 = time + (time2 - time) * param3;
										cy14 = b2 + (nb2 - b2) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, r, nr, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									SetBezier(input, timeline, bezier++, frame, 1, time, time2, g, ng, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
									SetBezier(input, timeline, bezier++, frame, 2, time, time2, b, nb, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
									SetBezier(input, timeline, bezier++, frame, 3, time, time2, a, na, 1, isOldCurveSpine, cx7, cy7, cx8, cy8);
									SetBezier(input, timeline, bezier++, frame, 4, time, time2, r2, nr2, 1, isOldCurveSpine, cx9, cy9, cx10, cy10);
									SetBezier(input, timeline, bezier++, frame, 5, time, time2, g2, ng2, 1, isOldCurveSpine, cx11, cy11, cx12, cy12);
									SetBezier(input, timeline, bezier++, frame, 6, time, time2, b2, nb2, 1, isOldCurveSpine, cx13, cy13, cx14, cy14);
									break;
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								a = na;
								r2 = nr2;
								g2 = ng2;
								b2 = nb2;
							}
							timelines.Add(timeline);
							break;
						}
						case SLOT_RGB2: {
							int bezierCountRGB2 = isOldCurveSpine ? frameCount * 6 : input.ReadInt(true);
							RGB2Timeline timeline = new RGB2Timeline(frameCount, bezierCountRGB2, slotIndex);
							float time = input.ReadFloat();
							float r = input.Read() / 255f, g = input.Read() / 255f, b = input.Read() / 255f;
							float r2 = input.Read() / 255f, g2 = input.Read() / 255f, b2 = input.Read() / 255f;
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, r, g, b, r2, g2, b2);
								if (frame == frameLast) break;
								float time2 = 0;
								float nr = 0, ng = 0, nb = 0;
								float nr2 = 0, ng2 = 0, nb2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								nr = input.Read() / 255f;
								ng = input.Read() / 255f;
								nb = input.Read() / 255f;
								nr2 = input.Read() / 255f;
								ng2 = input.Read() / 255f;
								nb2 = input.Read() / 255f;
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
									float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
									float cx7 = 0, cy7 = 0, cx8 = 0, cy8 = 0;
									float cx9 = 0, cy9 = 0, cx10 = 0, cy10 = 0;
									float cx11 = 0, cy11 = 0, cx12 = 0, cy12 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = r + (nr - r) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = r + (nr - r) * param4;

										cx3 = time + (time2 - time) * param1;
										cy3 = g + (ng - g) * param2;
										cx4 = time + (time2 - time) * param3;
										cy4 = g + (ng - g) * param4;

										cx5 = time + (time2 - time) * param1;
										cy5 = b + (nb - b) * param2;
										cx6 = time + (time2 - time) * param3;
										cy6 = b + (nb - b) * param4;

										cx7 = time + (time2 - time) * param1;
										cy7 = r2 + (nr2 - r2) * param2;
										cx8 = time + (time2 - time) * param3;
										cy8 = r2 + (nr2 - r2) * param4;

										cx9 = time + (time2 - time) * param1;
										cy9 = g2 + (ng2 - g2) * param2;
										cx10 = time + (time2 - time) * param3;
										cy10 = g2 + (ng2 - g2) * param4;

										cx11 = time + (time2 - time) * param1;
										cy11 = b2 + (nb2 - b2) * param2;
										cx12 = time + (time2 - time) * param3;
										cy12 = b2 + (nb2 - b2) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, r, nr, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									SetBezier(input, timeline, bezier++, frame, 1, time, time2, g, ng, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
									SetBezier(input, timeline, bezier++, frame, 2, time, time2, b, nb, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
									SetBezier(input, timeline, bezier++, frame, 3, time, time2, r2, nr2, 1, isOldCurveSpine, cx7, cy7, cx8, cy8);
									SetBezier(input, timeline, bezier++, frame, 4, time, time2, g2, ng2, 1, isOldCurveSpine, cx9, cy9, cx10, cy10);
									SetBezier(input, timeline, bezier++, frame, 5, time, time2, b2, nb2, 1, isOldCurveSpine, cx11, cy11, cx12, cy12);
									break;
								}
								time = time2;
								r = nr;
								g = ng;
								b = nb;
								r2 = nr2;
								g2 = ng2;
								b2 = nb2;
							}
							timelines.Add(timeline);
							break;
						}
						case SLOT_ALPHA: {
							int bezierCountA = isOldCurveSpine ? frameCount : input.ReadInt(true);
							AlphaTimeline timeline = new AlphaTimeline(frameCount, bezierCountA, slotIndex);
							float time = input.ReadFloat(), a = input.Read() / 255f;
							for (int frame = 0, bezier = 0; ; frame++) {
								timeline.SetFrame(frame, time, a);
								if (frame == frameLast) break;
								float time2 = 0;
								float a2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								a2 = input.Read() / 255f;
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = a + (a2 - a) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = a + (a2 - a) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, a, a2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									break;
								}
								time = time2;
								a = a2;
							}
							timelines.Add(timeline);
							break;
						}
					}
				}
			}

			// Bone timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				int boneIndex = input.ReadInt(true);
				for (int ii = 0, nn = input.ReadInt(true); ii < nn; ii++) {
					int type = input.ReadByte(), frameCount = input.ReadInt(true);
					int bezierCount = isOldCurveSpine ? frameCount : input.ReadInt(true);
					
					//Old Spine Reflect
                    if (isOldCurveSpine)
                    {
                        switch (type)
                        {
							case 2:
								type = 4;
								break;
							case 3:
								type = 7;
								break;
						}
                    }

					switch (type) {
					case BONE_ROTATE:
						limitRotation = true;
						timelines.Add(ReadTimeline(input, new RotateTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						limitRotation = false;
						break;
					case BONE_TRANSLATE:
						if (isOldCurveSpine)
						{
							bezierCount = bezierCount << 1;
						}
						timelines.Add(ReadTimeline(input, new TranslateTimeline(frameCount, bezierCount, boneIndex), scale, isOldCurveSpine));
						break;
					case BONE_TRANSLATEX:
						timelines.Add(ReadTimeline(input, new TranslateXTimeline(frameCount, bezierCount, boneIndex), scale, isOldCurveSpine));
						break;
					case BONE_TRANSLATEY:
						timelines.Add(ReadTimeline(input, new TranslateYTimeline(frameCount, bezierCount, boneIndex), scale, isOldCurveSpine));
						break;
					case BONE_SCALE:
						if (isOldCurveSpine)
						{
							bezierCount = bezierCount << 1;
						}
						timelines.Add(ReadTimeline(input, new ScaleTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					case BONE_SCALEX:
						timelines.Add(ReadTimeline(input, new ScaleXTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					case BONE_SCALEY:
						timelines.Add(ReadTimeline(input, new ScaleYTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					case BONE_SHEAR:
						if (isOldCurveSpine)
						{
							bezierCount = bezierCount << 1;
						}
						timelines.Add(ReadTimeline(input, new ShearTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					case BONE_SHEARX:
						timelines.Add(ReadTimeline(input, new ShearXTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					case BONE_SHEARY:
						timelines.Add(ReadTimeline(input, new ShearYTimeline(frameCount, bezierCount, boneIndex), 1, isOldCurveSpine));
						break;
					}
				}
			}

			// IK constraint timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				int index = input.ReadInt(true), frameCount = input.ReadInt(true), frameLast = frameCount - 1;
				int bezierCount = isOldCurveSpine ? frameCount : input.ReadInt(true);
				IkConstraintTimeline timeline = new IkConstraintTimeline(frameCount, bezierCount, index);
				float time = input.ReadFloat();
				float mix = input.ReadFloat();
				float softness = actualVersionID > 37 ? input.ReadFloat() * scale : 1;
				for (int frame = 0, bezier = 0; ; frame++) {
					int blendDirection = input.ReadSByte();
					bool compress = actualVersionID < 37 ? false : input.ReadBoolean();
					bool stretch = actualVersionID < 37 ? false : input.ReadBoolean();
					timeline.SetFrame(frame, time, mix, softness, blendDirection, compress, stretch);
					if (frame == frameLast) break;
					float time2 = 0, mix2 = 0, softness2 = 0;
					float param1 = 0;
					float param2 = 0;
					float param3 = 0;
					float param4 = 0;
					byte oldCurveType = 0;
					if (isOldCurveSpine)
					{
						oldCurveType = input.ReadByte();
						switch (oldCurveType)
						{
							case CURVE_BEZIER:
								param1 = input.ReadFloat();
								param2 = input.ReadFloat();
								param3 = input.ReadFloat();
								param4 = input.ReadFloat();
								break;
						}
					}
					time2 = input.ReadFloat();
					mix2 = input.ReadFloat();
					softness2 = actualVersionID > 37 ? input.ReadFloat() * scale : 1;
					switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
					case CURVE_STEPPED:
						timeline.SetStepped(frame);
						break;
					case CURVE_BEZIER:
						float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
						float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
						if (isOldCurveSpine)
						{
							cx1 = time + (time2 - time) * param1;
							cy1 = mix + (mix2 - mix) * param2;
							cx2 = time + (time2 - time) * param3;
							cy2 = mix + (mix2 - mix) * param4;

							cx3 = time + (time2 - time) * param1;
							cy3 = softness + (softness2 - softness) * param2;
							cx4 = time + (time2 - time) * param3;
							cy4 = softness + (softness2 - softness) * param4;
						}
						SetBezier(input, timeline, bezier++, frame, 0, time, time2, mix, mix2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
						SetBezier(input, timeline, bezier++, frame, 1, time, time2, softness, softness2, scale, isOldCurveSpine, cx3, cy3, cx4, cy4);
						break;
					}
					time = time2;
					mix = mix2;
					softness = softness2;
				}
				timelines.Add(timeline);
			}

			// Transform constraint timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				int index = input.ReadInt(true), frameCount = input.ReadInt(true), frameLast = frameCount - 1;
				int bezierCount = isOldCurveSpine ? frameCount * 6 : input.ReadInt(true);
				TransformConstraintTimeline timeline = new TransformConstraintTimeline(frameCount, bezierCount, index);

				float time = input.ReadFloat();
				float mixRotate = input.ReadFloat();
				float mixX = input.ReadFloat();
				float mixY = isOldCurveSpine ? mixX : input.ReadFloat();
				float mixScaleX = input.ReadFloat();
				float mixScaleY = isOldCurveSpine ? mixScaleX : input.ReadFloat();
				float mixShearY = input.ReadFloat();

				for (int frame = 0, bezier = 0; ; frame++) {
					timeline.SetFrame(frame, time, mixRotate, mixX, mixY, mixScaleX, mixScaleY, mixShearY);
					if (frame == frameLast) break;
					float time2 = 0, mixRotate2 = 0, mixX2 = 0, mixY2 = 0,
					mixScaleX2 = 0, mixScaleY2 = 0, mixShearY2 = 0;
					float param1 = 0;
					float param2 = 0;
					float param3 = 0;
					float param4 = 0;
					byte oldCurveType = 0;
					if (isOldCurveSpine)
					{
						oldCurveType = input.ReadByte();
						switch (oldCurveType)
						{
							case CURVE_BEZIER:
								param1 = input.ReadFloat();
								param2 = input.ReadFloat();
								param3 = input.ReadFloat();
								param4 = input.ReadFloat();
								break;
						}
					}

					time2 = input.ReadFloat();
					mixRotate2 = input.ReadFloat();
					mixX2 = input.ReadFloat();
					mixY2 = isOldCurveSpine ? mixX2 : input.ReadFloat();
					mixScaleX2 = input.ReadFloat();
					mixScaleY2 = isOldCurveSpine ? mixScaleX2 : input.ReadFloat();
					mixShearY2 = input.ReadFloat();

					switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
					case CURVE_STEPPED:
						timeline.SetStepped(frame);
						break;
					case CURVE_BEZIER:
						float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
						float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
						float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
						float cx7 = 0, cy7 = 0, cx8 = 0, cy8 = 0;
						float cx9 = 0, cy9 = 0, cx10 = 0, cy10 = 0;
						float cx11 = 0, cy11 = 0, cx12 = 0, cy12 = 0;
						if (isOldCurveSpine)
						{
							cx1 = time + (time2 - time) * param1;
							cy1 = mixRotate + (mixRotate2 - mixRotate) * param2;
							cx2 = time + (time2 - time) * param3;
							cy2 = mixRotate + (mixRotate2 - mixRotate) * param4;

							cx3 = time + (time2 - time) * param1;
							cy3 = mixX + (mixX2 - mixX) * param2;
							cx4 = time + (time2 - time) * param3;
							cy4 = mixX + (mixX2 - mixX) * param4;

							cx5 = time + (time2 - time) * param1;
							cy5 = mixY + (mixY2 - mixY) * param2;
							cx6 = time + (time2 - time) * param3;
							cy6 = mixY + (mixY2 - mixY) * param4;

							cx7 = time + (time2 - time) * param1;
							cy7 = mixScaleX + (mixScaleX2 - mixScaleX) * param2;
							cx8 = time + (time2 - time) * param3;
							cy8 = mixScaleX + (mixScaleX2 - mixScaleX) * param4;

							cx9 = time + (time2 - time) * param1;
							cy9 = mixScaleY + (mixScaleY2 - mixScaleY) * param2;
							cx10 = time + (time2 - time) * param3;
							cy10 = mixScaleY + (mixScaleY2 - mixScaleY) * param4;

							cx11 = time + (time2 - time) * param1;
							cy11 = mixShearY + (mixShearY2 - mixShearY) * param2;
							cx12 = time + (time2 - time) * param3;
							cy12 = mixShearY + (mixShearY2 - mixShearY) * param4;
						}
						SetBezier(input, timeline, bezier++, frame, 0, time, time2, mixRotate, mixRotate2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
						SetBezier(input, timeline, bezier++, frame, 1, time, time2, mixX, mixX2, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
						SetBezier(input, timeline, bezier++, frame, 2, time, time2, mixY, mixY2, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
						SetBezier(input, timeline, bezier++, frame, 3, time, time2, mixScaleX, mixScaleX2, 1, isOldCurveSpine, cx7, cy7, cx8, cy8);
						SetBezier(input, timeline, bezier++, frame, 4, time, time2, mixScaleY, mixScaleY2, 1, isOldCurveSpine, cx9, cy9, cx10, cy10);
						SetBezier(input, timeline, bezier++, frame, 5, time, time2, mixShearY, mixShearY2, 1, isOldCurveSpine, cx11, cy11, cx12, cy12);
						break;
					}
					time = time2;
					mixRotate = mixRotate2;
					mixX = mixX2;
					mixY = mixY2;
					mixScaleX = mixScaleX2;
					mixScaleY = mixScaleY2;
					mixShearY = mixShearY2;
				}
				timelines.Add(timeline);
			}

			// Path constraint timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				int index = input.ReadInt(true);
				PathConstraintData data = skeletonData.pathConstraints.Items[index];
				for (int ii = 0, nn = input.ReadInt(true); ii < nn; ii++) {
					int timelineType = 0;
					if(actualVersionID < 39)
                    {
						timelineType = input.ReadSByte();
					}
					int frameCount = input.ReadInt(true);
					switch (actualVersionID < 39 ? timelineType : input.ReadByte()) {
					case PATH_POSITION:
						int bezierCountPos = isOldCurveSpine ? frameCount : input.ReadInt(true);
						timelines
							.Add(ReadTimeline(input, new PathConstraintPositionTimeline(frameCount, bezierCountPos, index),
								data.positionMode == PositionMode.Fixed ? scale : 1, isOldCurveSpine));
						break;
					case PATH_SPACING:
						int bezierCountSpace = isOldCurveSpine ? frameCount : input.ReadInt(true);
						timelines
							.Add(ReadTimeline(input, new PathConstraintSpacingTimeline(frameCount, bezierCountSpace, index),
								data.spacingMode == SpacingMode.Length || data.spacingMode == SpacingMode.Fixed ? scale : 1, isOldCurveSpine));
						break;
					case PATH_MIX:
						int bezierCountMix = isOldCurveSpine ? frameCount * 3 : input.ReadInt(true);
						PathConstraintMixTimeline timeline = new PathConstraintMixTimeline(frameCount, bezierCountMix,
							index);
						float time = input.ReadFloat(), mixRotate = input.ReadFloat(), mixX = input.ReadFloat(); 
						float mixY = isOldCurveSpine ? mixX : input.ReadFloat();
						for (int frame = 0, bezier = 0, frameLast = timeline.FrameCount - 1; ; frame++) {
							timeline.SetFrame(frame, time, mixRotate, mixX, mixY);
							if (frame == frameLast) break;
							float time2 = 0, mixRotate2 = 0, mixX2 = 0,
							mixY2 = 0;
							float param1 = 0;
							float param2 = 0;
							float param3 = 0;
							float param4 = 0;
							byte oldCurveType = 0;
							if (isOldCurveSpine)
							{
								oldCurveType = input.ReadByte();
								switch (oldCurveType)
								{
									case CURVE_BEZIER:
										param1 = input.ReadFloat();
										param2 = input.ReadFloat();
										param3 = input.ReadFloat();
										param4 = input.ReadFloat();
										break;
								}
							}
							time2 = input.ReadFloat();
							mixRotate2 = input.ReadFloat();
							mixX2 = input.ReadFloat();
							mixY2 = isOldCurveSpine ? mixX2 : input.ReadFloat();
							switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
							case CURVE_STEPPED:
								timeline.SetStepped(frame);
								break;
							case CURVE_BEZIER:
								float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
								float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
								float cx5 = 0, cy5 = 0, cx6 = 0, cy6 = 0;
								if (isOldCurveSpine)
								{
									cx1 = time + (time2 - time) * param1;
									cy1 = mixRotate + (mixRotate2 - mixRotate) * param2;
									cx2 = time + (time2 - time) * param3;
									cy2 = mixRotate + (mixRotate2 - mixRotate) * param4;

									cx3 = time + (time2 - time) * param1;
									cy3 = mixX + (mixX2 - mixX) * param2;
									cx4 = time + (time2 - time) * param3;
									cy4 = mixX + (mixX2 - mixX) * param4;

									cx5 = time + (time2 - time) * param1;
									cy5 = mixY + (mixY2 - mixY) * param2;
									cx6 = time + (time2 - time) * param3;
									cy6 = mixY + (mixY2 - mixY) * param4;
								}
								SetBezier(input, timeline, bezier++, frame, 0, time, time2, mixRotate, mixRotate2, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
								SetBezier(input, timeline, bezier++, frame, 1, time, time2, mixX, mixX2, 1, isOldCurveSpine, cx3, cy3, cx4, cy4);
								SetBezier(input, timeline, bezier++, frame, 2, time, time2, mixY, mixY2, 1, isOldCurveSpine, cx5, cy5, cx6, cy6);
								break;
							}
							time = time2;
							mixRotate = mixRotate2;
							mixX = mixX2;
							mixY = mixY2;
						}
						timelines.Add(timeline);
						break;
					}
				}
			}

			// Attachment timelines.
			for (int i = 0, n = input.ReadInt(true); i < n; i++) {
				Skin skin = skeletonData.skins.Items[input.ReadInt(true)];
				for (int ii = 0, nn = input.ReadInt(true); ii < nn; ii++) {
					int slotIndex = input.ReadInt(true);
					for (int iii = 0, nnn = input.ReadInt(true); iii < nnn; iii++) {
						String attachmentName = input.ReadStringRef();
						Attachment attachment = skin.GetAttachment(slotIndex, attachmentName);
						if (attachment == null) throw new SerializationException("Timeline attachment not found: " + attachmentName);

						int timelineType = isOldCurveSpine ? 0 : input.ReadByte();
						int frameCount = input.ReadInt(true), frameLast = frameCount - 1;
						switch (timelineType) {
						case ATTACHMENT_DEFORM: {
							VertexAttachment vertexAttachment = (VertexAttachment)attachment;
							bool weighted = vertexAttachment.Bones != null;
							float[] vertices = vertexAttachment.Vertices;
							int deformLength = weighted ? (vertices.Length / 3) << 1 : vertices.Length;
							int bezierCount = isOldCurveSpine ? frameCount : input.ReadInt(true);
							DeformTimeline timeline = new DeformTimeline(frameCount, bezierCount, slotIndex, vertexAttachment);

							float time = input.ReadFloat();
							for (int frame = 0, bezier = 0; ; frame++) {
								float[] deform;
								int end = input.ReadInt(true);
								if (end == 0)
									deform = weighted ? new float[deformLength] : vertices;
								else {
									deform = new float[deformLength];
									int start = input.ReadInt(true);
									end += start;
									if (scale == 1) {
										for (int v = start; v < end; v++)
											deform[v] = input.ReadFloat();
									} else {
										for (int v = start; v < end; v++)
											deform[v] = input.ReadFloat() * scale;
									}
									if (!weighted) {
										for (int v = 0, vn = deform.Length; v < vn; v++)
											deform[v] += vertices[v];
									}
								}
								timeline.SetFrame(frame, time, deform);
								if (frame == frameLast) break;
								float time2 = 0;
								float param1 = 0;
								float param2 = 0;
								float param3 = 0;
								float param4 = 0;
								byte oldCurveType = 0;
								if (isOldCurveSpine)
								{
									oldCurveType = input.ReadByte();
									switch (oldCurveType)
									{
										case CURVE_BEZIER:
											param1 = input.ReadFloat();
											param2 = input.ReadFloat();
											param3 = input.ReadFloat();
											param4 = input.ReadFloat();
											break;
									}
								}
								time2 = input.ReadFloat();
								switch (isOldCurveSpine ? oldCurveType : input.ReadByte()) {
								case CURVE_STEPPED:
									timeline.SetStepped(frame);
									break;
								case CURVE_BEZIER:
									float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
									if (isOldCurveSpine)
									{
										cx1 = time + (time2 - time) * param1;
										cy1 = 0 + (1 - 0) * param2;
										cx2 = time + (time2 - time) * param3;
										cy2 = 0 + (1 - 0) * param4;
									}
									SetBezier(input, timeline, bezier++, frame, 0, time, time2, 0, 1, 1, isOldCurveSpine, cx1, cy1, cx2, cy2);
									break;
								}
								time = time2;
							}
							timelines.Add(timeline);
							break;
						}
						case ATTACHMENT_SEQUENCE: {
							SequenceTimeline timeline = new SequenceTimeline(frameCount, slotIndex, attachment);
							for (int frame = 0; frame < frameCount; frame++) {
								float time = input.ReadFloat();
								int modeAndIndex = input.ReadInt();
								timeline.SetFrame(frame, time, (SequenceMode)(modeAndIndex & 0xf), modeAndIndex >> 4,
									input.ReadFloat());
							}
							timelines.Add(timeline);
							break;
						} // end case
						} // end switch
					}
				}
			}
			
			// Draw order timeline.
			int drawOrderCount = input.ReadInt(true);
			if (drawOrderCount > 0) {
				DrawOrderTimeline timeline = new DrawOrderTimeline(drawOrderCount);
				int slotCount = skeletonData.slots.Count;
				for (int i = 0; i < drawOrderCount; i++) {
					float time = input.ReadFloat();
					int offsetCount = input.ReadInt(true);
					int[] drawOrder = new int[slotCount];
					for (int ii = slotCount - 1; ii >= 0; ii--)
						drawOrder[ii] = -1;
					int[] unchanged = new int[slotCount - offsetCount];
					int originalIndex = 0, unchangedIndex = 0;
					for (int ii = 0; ii < offsetCount; ii++) {
						int slotIndex = input.ReadInt(true);
						// Collect unchanged items.
						while (originalIndex != slotIndex)
							unchanged[unchangedIndex++] = originalIndex++;
						// Set changed items.
						drawOrder[originalIndex + input.ReadInt(true)] = originalIndex++;
					}
					// Collect remaining unchanged items.
					while (originalIndex < slotCount)
						unchanged[unchangedIndex++] = originalIndex++;
					// Fill in unchanged items.
					for (int ii = slotCount - 1; ii >= 0; ii--)
						if (drawOrder[ii] == -1) drawOrder[ii] = unchanged[--unchangedIndex];
					timeline.SetFrame(i, time, drawOrder);
				}
				timelines.Add(timeline);
			}
			
			// Event timeline.
			int eventCount = input.ReadInt(true);
			if (eventCount > 0) {
				EventTimeline timeline = new EventTimeline(eventCount);
				for (int i = 0; i < eventCount; i++) {
					float time = input.ReadFloat();
					EventData eventData = skeletonData.events.Items[input.ReadInt(true)];
					Event e = new Event(time, eventData);
					e.intValue = input.ReadInt(false);
					e.floatValue = input.ReadFloat();
					e.stringValue = input.ReadBoolean() ? input.ReadString() : eventData.String;

					if(actualVersionID > 36)
                    {
						if (e.Data.AudioPath != null)
						{
							e.volume = input.ReadFloat();
							e.balance = input.ReadFloat();
						}
					}
					timeline.SetFrame(i, e);
				}
				timelines.Add(timeline);
			}
			
			float duration = 0;
			Timeline[] items = timelines.Items;
			for (int i = 0, n = timelines.Count; i < n; i++)
				duration = Math.Max(duration, items[i].Duration);

			return new Animation(name, timelines, duration);
		}

		/// <exception cref="IOException">Throws IOException when a read operation fails.</exception>
		private Timeline ReadTimeline (SkeletonInput input, CurveTimeline1 timeline, float scale, bool isOldSpine) {
			float time = input.ReadFloat(), value = input.ReadFloat() * scale;
			for (int frame = 0, bezier = 0, frameLast = timeline.FrameCount - 1; ; frame++) {
				timeline.SetFrame(frame, time, value);
				if (frame == frameLast) break;
				float time2 = 0, value2 = 0;
				float param1 = 0;
				float param2 = 0;
				float param3 = 0;
				float param4 = 0;
				byte oldCurveType = 0;
				if (isOldSpine)
				{
					oldCurveType = input.ReadByte();
					switch (oldCurveType)
					{
						case CURVE_BEZIER:
							param1 = input.ReadFloat();
							param2 = input.ReadFloat();
							param3 = input.ReadFloat();
							param4 = input.ReadFloat();
							break;
					}
				}

				time2 = input.ReadFloat();
				value2 = input.ReadFloat() * scale;

				if (limitRotation)
                {
					// Find Nearest Angle
					float diff = Math.Abs(value2 - value);

					float diff2 = Math.Abs((value2 - 360) - value);

					float diff3 = Math.Abs((value2 + 360) - value);

					float[] diffList = new float[3] { diff, diff2, diff3 };

					int minIndex = 0;
					float minValue = diffList[0];

					for(int i = 1; i < diffList.Length; i++)
                    {
						if(diffList[i] < minValue)
                        {
							minIndex = i;
							minValue = diffList[i];
						}
                    }

                    switch (minIndex)
                    {
						case 0:
							break;
						case 1:
							value2 -= 360;
							break;
						case 2:
							value2 += 360;
							break;
                    }
					//value2 = diff > diff2 ? (value2 - 360) : value2;
				}
					
				//float time2 = input.ReadFloat(), value2 = input.ReadFloat() * scale;
				switch (isOldSpine ? oldCurveType : input.ReadByte()) {
				case CURVE_STEPPED:
					timeline.SetStepped(frame);
					break;
				case CURVE_BEZIER:
					float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
					if (isOldSpine)
					{
						cx1 = time + (time2 - time) * param1;
						cy1 = value + (value2 - value) * param2;
						cx2 = time + (time2 - time) * param3;
						cy2 = value + (value2 - value) * param4;
					}
					SetBezier(input, timeline, bezier++, frame, 0, time, time2, value, value2, scale, isOldSpine, cx1, cy1, cx2, cy2);
					break;
				}
				time = time2;
				value = value2;
			}
			return timeline;
		}

		/// <exception cref="IOException">Throws IOException when a read operation fails.</exception>
		private Timeline ReadTimeline (SkeletonInput input, CurveTimeline2 timeline, float scale, bool isOldSpine) {
			float time = input.ReadFloat(), value1 = input.ReadFloat() * scale, value2 = input.ReadFloat() * scale;

			for (int frame = 0, bezier = 0, frameLast = timeline.FrameCount - 1; ; frame++) {
				timeline.SetFrame(frame, time, value1, value2);
				if (frame == frameLast) break;
				float time2 = 0, nvalue1 = 0, nvalue2 = 0;
				float param1 = 0;
				float param2 = 0;
				float param3 = 0;
				float param4 = 0;
				byte oldCurveType = 0;
                if (isOldSpine)
                {
					oldCurveType = input.ReadByte();

                    switch (oldCurveType)
                    {
						case CURVE_BEZIER:
							param1 = input.ReadFloat();
							param2 = input.ReadFloat();
							param3 = input.ReadFloat();
							param4 = input.ReadFloat();
							break;
					}
                }

				time2 = input.ReadFloat();
				nvalue1 = input.ReadFloat() * scale;
				nvalue2 = input.ReadFloat() * scale;

				switch (isOldSpine ? oldCurveType : input.ReadByte()) {
				case CURVE_STEPPED:
					timeline.SetStepped(frame);
					break;
				case CURVE_BEZIER:
					float cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0;
					float cx3 = 0, cy3 = 0, cx4 = 0, cy4 = 0;
					if (isOldSpine)
					{
						cx1 = time + (time2 - time) * param1;
						cy1 = value1 + (nvalue1 - value1) * param2;
						cx2 = time + (time2 - time) * param3;
						cy2 = value1 + (nvalue1 - value1) * param4;

						cx3 = time + (time2 - time) * param1;
						cy3 = value2 + (nvalue2 - value2) * param2;
						cx4 = time + (time2 - time) * param3;
						cy4 = value2 + (nvalue2 - value2) * param4;
                    }
					SetBezier(input, timeline, bezier++, frame, 0, time, time2, value1, nvalue1, scale, isOldSpine, cx1, cy1, cx2, cy2);
					SetBezier(input, timeline, bezier++, frame, 1, time, time2, value2, nvalue2, scale, isOldSpine, cx3, cy3, cx4, cy4);
					break;
				}
				time = time2;
				value1 = nvalue1;
				value2 = nvalue2;
			}
			return timeline;
		}

		/// <exception cref="IOException">Throws IOException when a read operation fails.</exception>
		void SetBezier (SkeletonInput input, CurveTimeline timeline, int bezier, int frame, int value, float time1, float time2,
			float value1, float value2, float scale,bool usePredefine, float cx1, float cy1, float cx2, float cy2) {

            if (!usePredefine)
            {
				cx1 = input.ReadFloat();
				cy1 = input.ReadFloat();
				cx2 = input.ReadFloat();
				cy2 = input.ReadFloat();
				cy1 *= scale;
				cy2 *= scale;
			}

			timeline.SetBezier(bezier, frame, value, time1, value1, cx1, cy1, cx2,
					cy2, time2, value2);
		}


		public static int GetVersionID(string version)
		{
			if (version.Contains("3.5."))
			{
				return 35;
			}

			if (version.Contains("3.6."))
			{
				return 36;
			}

			if (version.Contains("3.7."))
			{
				return 37;
			}

			if (version.Contains("3.8."))
			{
				return 38;
			}

			if (version.Contains("4.0."))
			{
				return 40;
			}

			if (version.Contains("4.1."))
			{
				return 41;
			}

			return -1;
		}


		internal class Vertices {
			public int[] bones;
			public float[] vertices;
		}

		internal class SkeletonInput {
			private byte[] chars = new byte[32];
			private byte[] bytesBigEndian = new byte[8];
			internal string[] strings;
			Stream input;
			private bool useStringRef = true;

			public SkeletonInput (Stream input) {
				this.input = input;
			}

			public int Read () {
				return input.ReadByte();
			}

			public byte ReadByte () {
				return (byte)input.ReadByte();
			}

			public sbyte ReadSByte () {
				int value = input.ReadByte();
				if (value == -1) throw new EndOfStreamException();
				return (sbyte)value;
			}

			public bool ReadBoolean () {
				return input.ReadByte() != 0;
			}

			public float ReadFloat () {
				input.Read(bytesBigEndian, 0, 4);
				chars[3] = bytesBigEndian[0];
				chars[2] = bytesBigEndian[1];
				chars[1] = bytesBigEndian[2];
				chars[0] = bytesBigEndian[3];
				return BitConverter.ToSingle(chars, 0);
			}

			public int ReadInt () {
				input.Read(bytesBigEndian, 0, 4);
				return (bytesBigEndian[0] << 24)
					+ (bytesBigEndian[1] << 16)
					+ (bytesBigEndian[2] << 8)
					+ bytesBigEndian[3];
			}

			public long ReadLong () {
				input.Read(bytesBigEndian, 0, 8);
				return ((long)(bytesBigEndian[0]) << 56)
					+ ((long)(bytesBigEndian[1]) << 48)
					+ ((long)(bytesBigEndian[2]) << 40)
					+ ((long)(bytesBigEndian[3]) << 32)
					+ ((long)(bytesBigEndian[4]) << 24)
					+ ((long)(bytesBigEndian[5]) << 16)
					+ ((long)(bytesBigEndian[6]) << 8)
					+ (long)(bytesBigEndian[7]);
			}

			public int ReadInt (bool optimizePositive) {
				int b = input.ReadByte();
				int result = b & 0x7F;
				if ((b & 0x80) != 0) {
					b = input.ReadByte();
					result |= (b & 0x7F) << 7;
					if ((b & 0x80) != 0) {
						b = input.ReadByte();
						result |= (b & 0x7F) << 14;
						if ((b & 0x80) != 0) {
							b = input.ReadByte();
							result |= (b & 0x7F) << 21;
							if ((b & 0x80) != 0) result |= (input.ReadByte() & 0x7F) << 28;
						}
					}
				}
				return optimizePositive ? result : ((result >> 1) ^ -(result & 1));
			}

			public string ReadString () {
				int byteCount = ReadInt(true);
				switch (byteCount) {
				case 0:
					return null;
				case 1:
					return "";
				}
				byteCount--;
				byte[] buffer = this.chars;
				if (buffer.Length < byteCount) buffer = new byte[byteCount];
				ReadFully(buffer, 0, byteCount);
				return System.Text.Encoding.UTF8.GetString(buffer, 0, byteCount);
			}

			///<return>May be null.</return>
			public String ReadStringRef () {
				if (!useStringRef)
					return ReadString();
				int index = ReadInt(true);
				return index == 0 ? null : strings[index - 1];
			}

			public void ReadFully (byte[] buffer, int offset, int length) {
				while (length > 0) {
					int count = input.Read(buffer, offset, length);
					if (count <= 0) throw new EndOfStreamException();
					offset += count;
					length -= count;
				}
			}

			public void DisableStringRef()
            {
				useStringRef = false;
			}

			/// <summary>Returns the version string of binary skeleton data.</summary>
			public string GetVersionString () {
				try {
					// try reading 4.0+ format
					long initialPosition = input.Position;
					ReadLong(); // long hash

					long stringPosition = input.Position;
					int stringByteCount = ReadInt(true);
					input.Position = stringPosition;
					if (stringByteCount <= 13) {
						string version = ReadString();
						if (char.IsDigit(version[0]))
							return version;
					}
					// fallback to old version format
					input.Position = initialPosition;
					return GetVersionStringOld3X();
				} catch (Exception e) {
					throw new ArgumentException("Stream does not contain valid binary Skeleton Data.\n" + e, "input");
				}
			}

			/// <summary>Returns old 3.8 and earlier format version string of binary skeleton data.</summary>
			public string GetVersionStringOld3X () {
				// Hash.
				int byteCount = ReadInt(true);
				if (byteCount > 1) input.Position += byteCount - 1;

				// Version.
				byteCount = ReadInt(true);
				if (byteCount > 1 && byteCount <= 13) {
					byteCount--;
					byte[] buffer = new byte[byteCount];
					ReadFully(buffer, 0, byteCount);
					return System.Text.Encoding.UTF8.GetString(buffer, 0, byteCount);
				}
				throw new ArgumentException("Stream does not contain valid binary Skeleton Data.");
			}
		}
	}
}

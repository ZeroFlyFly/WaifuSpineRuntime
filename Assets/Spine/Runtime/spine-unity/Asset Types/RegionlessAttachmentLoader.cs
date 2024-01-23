/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated July 28, 2023. Replaces all prior versions.
 *
 * Copyright (c) 2013-2023, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software or
 * otherwise create derivative works of the Spine Runtimes (collectively,
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
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE
 * SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using UnityEngine;

namespace Spine.Unity {

	public class RegionlessAttachmentLoader : AttachmentLoader {

		static AtlasRegion emptyRegion;
		static AtlasRegion EmptyRegion {
			get {
				if (emptyRegion == null) {
					emptyRegion = new AtlasRegion {
						name = "Empty AtlasRegion",
						page = new AtlasPage {
							name = "Empty AtlasPage",
							rendererObject = new Material(Shader.Find("Spine/Special/HiddenPass")) { name = "NoRender Material" }
						}
					};
				}
				return emptyRegion;
			}
		}

		public RegionAttachment NewRegionAttachment (Skin skin, string name, string path, Sequence sequence) {
			RegionAttachment attachment = new RegionAttachment(name) {
				Region = EmptyRegion
			};
			return attachment;
		}

		public MeshAttachment NewMeshAttachment (Skin skin, string name, string path, Sequence sequence) {
			MeshAttachment attachment = new MeshAttachment(name) {
				Region = EmptyRegion
			};
			return attachment;
		}

		public BoundingBoxAttachment NewBoundingBoxAttachment (Skin skin, string name) {
			return new BoundingBoxAttachment(name);
		}

		public PathAttachment NewPathAttachment (Skin skin, string name) {
			return new PathAttachment(name);
		}

		public PointAttachment NewPointAttachment (Skin skin, string name) {
			return new PointAttachment(name);
		}

		public ClippingAttachment NewClippingAttachment (Skin skin, string name) {
			return new ClippingAttachment(name);
		}
	}
}

// <author>QROST</author>
// <created>20260312</created>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace MeshyRevit.Models
{
    #region Text-to-3D

    public class TextTo3DPreviewRequest
    {
        [JsonProperty("mode")]
        public string Mode { get; set; } = "preview";

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("ai_model")]
        public string AiModel { get; set; } = "latest";

        [JsonProperty("model_type", NullValueHandling = NullValueHandling.Ignore)]
        public string ModelType { get; set; }

        [JsonProperty("topology")]
        public string Topology { get; set; } = "triangle";

        [JsonProperty("target_polycount")]
        public int TargetPolycount { get; set; } = 30000;

        [JsonProperty("should_remesh")]
        public bool ShouldRemesh { get; set; } = false;

        [JsonProperty("symmetry_mode")]
        public string SymmetryMode { get; set; } = "auto";

        [JsonProperty("pose_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string PoseMode { get; set; }
    }

    public class TextTo3DRefineRequest
    {
        [JsonProperty("mode")]
        public string Mode { get; set; } = "refine";

        [JsonProperty("preview_task_id")]
        public string PreviewTaskId { get; set; }

        [JsonProperty("enable_pbr")]
        public bool EnablePbr { get; set; } = true;

        [JsonProperty("ai_model")]
        public string AiModel { get; set; } = "latest";

        [JsonProperty("texture_prompt")]
        public string TexturePrompt { get; set; }
    }

    #endregion

    #region Image-to-3D

    public class ImageTo3DRequest
    {
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("ai_model")]
        public string AiModel { get; set; } = "latest";

        [JsonProperty("model_type", NullValueHandling = NullValueHandling.Ignore)]
        public string ModelType { get; set; }

        [JsonProperty("topology")]
        public string Topology { get; set; } = "triangle";

        [JsonProperty("target_polycount")]
        public int TargetPolycount { get; set; } = 30000;

        [JsonProperty("should_remesh")]
        public bool ShouldRemesh { get; set; } = false;

        [JsonProperty("save_pre_remeshed_model")]
        public bool SavePreRemeshedModel { get; set; } = false;

        [JsonProperty("should_texture")]
        public bool ShouldTexture { get; set; } = true;

        [JsonProperty("enable_pbr")]
        public bool EnablePbr { get; set; } = false;

        [JsonProperty("symmetry_mode")]
        public string SymmetryMode { get; set; } = "auto";

        [JsonProperty("pose_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string PoseMode { get; set; }

        [JsonProperty("image_enhancement", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ImageEnhancement { get; set; }

        [JsonProperty("remove_lighting", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RemoveLighting { get; set; }

        [JsonProperty("moderation")]
        public bool Moderation { get; set; } = false;
    }

    #endregion

    #region Multi-Image-to-3D

    public class MultiImageTo3DRequest
    {
        [JsonProperty("image_urls")]
        public List<string> ImageUrls { get; set; } = new List<string>();

        [JsonProperty("ai_model")]
        public string AiModel { get; set; } = "latest";

        [JsonProperty("topology")]
        public string Topology { get; set; } = "triangle";

        [JsonProperty("target_polycount")]
        public int TargetPolycount { get; set; } = 30000;

        [JsonProperty("should_remesh")]
        public bool ShouldRemesh { get; set; } = false;

        [JsonProperty("save_pre_remeshed_model")]
        public bool SavePreRemeshedModel { get; set; } = false;

        [JsonProperty("should_texture")]
        public bool ShouldTexture { get; set; } = true;

        [JsonProperty("enable_pbr")]
        public bool EnablePbr { get; set; } = false;

        [JsonProperty("symmetry_mode")]
        public string SymmetryMode { get; set; } = "auto";

        [JsonProperty("pose_mode", NullValueHandling = NullValueHandling.Ignore)]
        public string PoseMode { get; set; }

        [JsonProperty("image_enhancement", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ImageEnhancement { get; set; }

        [JsonProperty("remove_lighting", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RemoveLighting { get; set; }

        [JsonProperty("moderation")]
        public bool Moderation { get; set; } = false;
    }

    #endregion

    #region Task Response

    public class MeshyTaskCreateResponse
    {
        [JsonProperty("result")]
        public string Result { get; set; }
    }

    public class MeshyTaskStatus
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("model_urls")]
        public ModelUrls ModelUrls { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("texture_urls")]
        public List<TextureUrls> TextureUrlsList { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("started_at")]
        public long StartedAt { get; set; }

        [JsonProperty("created_at")]
        public long CreatedAt { get; set; }

        [JsonProperty("finished_at")]
        public long FinishedAt { get; set; }

        [JsonProperty("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonProperty("task_error")]
        public TaskError TaskError { get; set; }

        [JsonProperty("preceding_tasks")]
        public int PrecedingTasks { get; set; }
    }

    public class ModelUrls
    {
        [JsonProperty("glb")]
        public string Glb { get; set; }

        [JsonProperty("fbx")]
        public string Fbx { get; set; }

        [JsonProperty("obj")]
        public string Obj { get; set; }

        [JsonProperty("mtl")]
        public string Mtl { get; set; }

        [JsonProperty("usdz")]
        public string Usdz { get; set; }

        [JsonProperty("pre_remeshed_glb")]
        public string PreRemeshedGlb { get; set; }
    }

    public class TextureUrls
    {
        [JsonProperty("base_color")]
        public string BaseColor { get; set; }

        [JsonProperty("metallic")]
        public string Metallic { get; set; }

        [JsonProperty("normal")]
        public string Normal { get; set; }

        [JsonProperty("roughness")]
        public string Roughness { get; set; }
    }

    public class TaskError
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    #endregion

    #region Enums

    public enum MeshyTaskStatusEnum
    {
        PENDING,
        IN_PROGRESS,
        SUCCEEDED,
        FAILED,
        CANCELED
    }

    public enum PlacementMode
    {
        DirectShape,
        Family
    }

    public enum GenerationMode
    {
        TextTo3D,
        ImageTo3D,
        MultiImageTo3D
    }

    #endregion
}

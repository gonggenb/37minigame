export type AssetCategory =
  | "character"
  | "scene"
  | "item"
  | "animation"
  | "effect"
  | "ui";

export type JobStatus =
  | "draft"
  | "planning"
  | "planned"
  | "generating"
  | "processing"
  | "reviewing"
  | "ready"
  | "needs_input"
  | "exporting"
  | "exported"
  | "failed"
  | "cancelled"
  | "interrupted";

export interface ProjectConfig {
  schema_version: 1;
  project_id: string;
  display_name: string;
  visual_type: string;
  language: "zh-CN" | "en-US";
  models: {
    planner_model: string;
    review_model: string;
    image_model: string;
  };
  generation: {
    candidate_count: number;
    automatic_retry_count: number;
    image_quality: "low" | "medium" | "high" | "auto";
    transparency_mode: "postprocess" | "opaque";
  };
  review: {
    enabled: boolean;
    minimum_style_score: number;
    hard_constraints_required: boolean;
  };
}

export interface ProjectActivityItem {
  workflow: "static" | "sequence";
  category: AssetCategory;
  asset_id: string;
  name: string;
  status: string;
  run_id: string | null;
  updated_at: string;
}

export interface ProjectCategoryActivity {
  category: AssetCategory;
  task_count: number;
  recent: ProjectActivityItem[];
}

export interface ProjectActivitySummary {
  schema_version: 1;
  project_id: string;
  reference_count: number;
  categories: ProjectCategoryActivity[];
}

export interface JobRecord {
  schema_version: 1;
  job_id: string;
  project_id: string;
  kind: string;
  status: JobStatus;
  progress: number;
  message: string;
  attempt: number;
  max_attempts: number;
  error: string | null;
  created_at: string;
  updated_at: string;
}

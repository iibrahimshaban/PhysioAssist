import { Component, signal, OnInit, inject, computed, DestroyRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ToolbarModule } from 'primeng/toolbar';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { TooltipModule } from 'primeng/tooltip';
import { SelectButtonModule } from 'primeng/selectbutton';
import { AccordionModule } from 'primeng/accordion';
import { DividerModule } from 'primeng/divider';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IntakeApiService } from '../../services/intake-api.service';
import { DynamicFormEngineService } from '../../services/dynamic-form-engine.service';
import { SnackbarService } from '../../../../Core/Services/snackbar.service';
import {
  FormSchemaResponse,
  DynamicFormSchemaDto,
  FormSectionDto,
  FormGroupDto,
  FormQuestionDto,
  QuestionConditionDto,
  ValidationRuleDto
} from '../../models';

type BuilderMode = 'schema' | 'section' | 'group' | 'question' | null;

interface QuestionTypeOption {
  label: string;
  value: string;
  icon: string;
}

@Component({
  selector: 'app-schema-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    CardModule,
    InputTextModule,
    TextareaModule,
    CheckboxModule,
    SelectModule,
    InputNumberModule,
    TooltipModule,
    SelectButtonModule,
    AccordionModule,
    DividerModule
  ],
  template: `
    <div class="h-screen flex flex-col" style="background: #f8fafc;">
      <!-- ── Top Toolbar ─────────────────────────────────────── -->
      <div class="builder-toolbar">
        <div class="flex items-center gap-3 min-w-0">
          <p-button
            icon="pi pi-arrow-left"
            [text]="true"
            [rounded]="true"
            severity="secondary"
            (onClick)="goBack()"
            pTooltip="Back to Schemas"
            tooltipPosition="right">
          </p-button>
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <span class="builder-mode-badge">
                {{ isEditMode() ? 'Edit Mode' : 'New Schema' }}
              </span>
              @if (schemaVersion() > 1) {
                <span class="version-badge">v{{ schemaVersion() }}</span>
              }
            </div>
            <h2 class="builder-title">
              {{ selectedSchema()?.name || schemaName || 'Untitled Schema' }}
            </h2>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <p-button
            label="Cancel"
            icon="pi pi-times"
            [text]="true"
            severity="secondary"
            (onClick)="goBack()"
            styleClass="hidden sm:inline-flex">
          </p-button>
          <p-button
            label="Save Draft"
            icon="pi pi-save"
            [outlined]="true"
            severity="secondary"
            (onClick)="saveDraft()"
            [loading]="saving()">
          </p-button>
          <p-button
            label="Publish"
            icon="pi pi-send"
            severity="success"
            (onClick)="publish()"
            [loading]="publishing()">
          </p-button>
        </div>
      </div>

      <!-- ── Main 3-Column Layout ───────────────────────────── -->
      <div class="flex-1 flex overflow-hidden">

        <!-- Left Sidebar: Schema Tree -->
        <div class="builder-sidebar-left">
          <div class="sidebar-section-header">
            <i class="pi pi-sitemap text-xs text-surface-400"></i>
            <span>Structure</span>
          </div>

          <div class="sidebar-tree">
            <!-- Schema Root Node -->
            <div
              class="tree-node tree-node-root"
              [class.tree-node-selected]="selectedItem() === 'schema'"
              (click)="selectItem('schema', null)">
              <div class="tree-node-icon" style="background: #eef2ff; color: #6366f1;">
                <i class="pi pi-cog text-xs"></i>
              </div>
              <span class="tree-node-label font-semibold">Schema Settings</span>
            </div>

            <!-- Sections -->
            @for (section of formSchema().sections; track section.sectionId) {
              <div class="tree-section">
                <!-- Section Node -->
                <div
                  class="tree-node"
                  [class.tree-node-selected]="selectedItem() === section.sectionId"
                  (click)="selectItem('section', section)">
                  <button
                    class="tree-chevron"
                    (click)="toggleSection(section.sectionId); $event.stopPropagation()"
                    [class.tree-chevron-open]="isSectionExpanded(section.sectionId)">
                    <i class="pi pi-chevron-right text-xs"></i>
                  </button>
                  <div class="tree-node-icon" style="background: #fef3c7; color: #d97706;">
                    <i class="pi pi-folder text-xs"></i>
                  </div>
                  <span class="tree-node-label flex-1 truncate">{{ section.title }}</span>
                  <button
                    class="tree-action-btn"
                    (click)="deleteSection(section.sectionId); $event.stopPropagation()"
                    pTooltip="Delete section"
                    tooltipPosition="right">
                    <i class="pi pi-trash text-xs"></i>
                  </button>
                </div>

                <!-- Groups -->
                @if (isSectionExpanded(section.sectionId)) {
                  <div class="tree-children" style="animation: fadeIn 0.15s ease-out;">
                    @for (group of section.groups; track group.groupId) {
                      <div class="tree-group">
                        <!-- Group Node -->
                        <div
                          class="tree-node"
                          [class.tree-node-selected]="selectedItem() === group.groupId"
                          (click)="selectItem('group', group)">
                          <button
                            class="tree-chevron"
                            (click)="toggleGroup(group.groupId); $event.stopPropagation()"
                            [class.tree-chevron-open]="isGroupExpanded(group.groupId)">
                            <i class="pi pi-chevron-right text-xs"></i>
                          </button>
                          <div class="tree-node-icon" style="background: #dbeafe; color: #3b82f6;">
                            <i class="pi pi-th-large text-xs"></i>
                          </div>
                          <span class="tree-node-label flex-1 truncate">{{ group.title }}</span>
                          <button
                            class="tree-action-btn"
                            (click)="deleteGroup(section.sectionId, group.groupId); $event.stopPropagation()"
                            pTooltip="Delete group"
                            tooltipPosition="right">
                            <i class="pi pi-trash text-xs"></i>
                          </button>
                        </div>

                        <!-- Questions -->
                        @if (isGroupExpanded(group.groupId)) {
                          <div class="tree-children" style="animation: fadeIn 0.15s ease-out;">
                            @for (question of group.questions; track question.questionId) {
                              <div
                                class="tree-node"
                                [class.tree-node-selected]="selectedItem() === question.questionId"
                                (click)="selectItem('question', question)">
                                <div class="tree-node-icon" style="background: #f0fdf4; color: #22c55e;">
                                  <i class="pi pi-question text-xs"></i>
                                </div>
                                <span class="tree-node-label flex-1 truncate text-xs">{{ question.text }}</span>
                                <button
                                  class="tree-action-btn"
                                  (click)="deleteQuestion(section.sectionId, group.groupId, question.questionId); $event.stopPropagation()"
                                  pTooltip="Delete question"
                                  tooltipPosition="right">
                                  <i class="pi pi-trash text-xs"></i>
                                </button>
                              </div>
                            }
                            <!-- Add Question -->
                            <button
                              class="tree-add-btn"
                              (click)="addQuestion(section.sectionId, group.groupId)">
                              <i class="pi pi-plus text-xs"></i>
                              <span>Add Question</span>
                            </button>
                          </div>
                        }
                      </div>
                    }
                    <!-- Add Group -->
                    <button
                      class="tree-add-btn ml-2"
                      (click)="addGroup(section.sectionId)">
                      <i class="pi pi-plus text-xs"></i>
                      <span>Add Group</span>
                    </button>
                  </div>
                }
              </div>
            }

            <!-- Add Section -->
            <button class="tree-add-section-btn" (click)="addSection()">
              <i class="pi pi-plus"></i>
              <span>Add Section</span>
            </button>
          </div>
        </div>

        <!-- Center Canvas: Live Preview -->
        <div class="builder-canvas">
          <div class="canvas-inner">
            <!-- Empty state -->
            @if (formSchema().sections.length === 0) {
              <div class="canvas-empty-state">
                <div style="width:5rem;height:5rem;border-radius:50%;background:linear-gradient(135deg,#eef2ff,#f3e8ff);display:flex;align-items:center;justify-content:center;margin:0 auto 1.25rem;">
                  <i class="pi pi-file-edit" style="font-size:2rem;color:#8b5cf6;"></i>
                </div>
                <h3 class="canvas-empty-title">Start Building Your Form</h3>
                <p class="canvas-empty-text">
                  Add sections from the left panel to start building your intake form. Questions and groups will appear here as you build.
                </p>
                <p-button
                  label="Add First Section"
                  icon="pi pi-plus"
                  severity="primary"
                  (onClick)="addSection()">
                </p-button>
              </div>
            } @else {
              <!-- Schema Preview -->
              <div class="space-y-5 stagger-children">
                @for (section of formSchema().sections; track section.sectionId) {
                  <div
                    class="preview-section-card"
                    [class.preview-section-selected]="selectedItem() === section.sectionId"
                    (click)="selectItem('section', section)">

                    <!-- Section Header -->
                    <div class="preview-section-header">
                      <div class="preview-section-accent"></div>
                      <div class="flex-1">
                        <h3 class="preview-section-title">{{ section.title }}</h3>
                        @if (section.description) {
                          <p class="preview-section-desc">{{ section.description }}</p>
                        }
                      </div>
                      <span class="preview-count-badge">
                        {{ section.groups.length }} group{{ section.groups.length !== 1 ? 's' : '' }}
                      </span>
                    </div>

                    <!-- Groups -->
                    <div class="preview-section-body">
                      @for (group of section.groups; track group.groupId) {
                        <div
                          class="preview-group-card"
                          [class.preview-group-selected]="selectedItem() === group.groupId"
                          (click)="selectItem('group', group); $event.stopPropagation()">

                          @if (group.title !== 'New Group' || group.description) {
                            <div class="preview-group-header">
                              <i class="pi pi-th-large text-xs text-surface-400"></i>
                              <span class="preview-group-title">{{ group.title }}</span>
                              @if (group.description) {
                                <span class="preview-group-desc ml-1">— {{ group.description }}</span>
                              }
                            </div>
                          }

                          <!-- Questions -->
                          <div class="space-y-3">
                            @for (question of group.questions; track question.questionId) {
                              <div
                                class="preview-question-card"
                                [class.preview-question-selected]="selectedItem() === question.questionId"
                                (click)="selectItem('question', question); $event.stopPropagation()">

                                <div class="flex items-start gap-2 mb-2">
                                  <span class="preview-question-type-badge">
                                    <i [class]="getQuestionTypeIcon(question.type)"></i>
                                    {{ question.type }}
                                  </span>
                                  @if (question.required) {
                                    <span class="preview-required-badge">Required</span>
                                  }
                                  @if (question.conditions?.length) {
                                    <span class="preview-conditional-badge">
                                      <i class="pi pi-sliders-h text-xs"></i>
                                      Conditional
                                    </span>
                                  }
                                </div>

                                <label class="preview-question-label">
                                  {{ question.text }}
                                  @if (question.required) {
                                    <span class="text-red-400 ml-0.5">*</span>
                                  }
                                </label>

                                @if (question.description) {
                                  <p class="preview-question-desc">{{ question.description }}</p>
                                }

                                <!-- Preview control mockup -->
                                <div class="preview-control-wrapper">
                                  @switch (question.type) {
                                    @case ('text') {
                                      <div class="preview-input">Sample text input</div>
                                    }
                                    @case ('number') {
                                      <div class="preview-input">0</div>
                                    }
                                    @case ('email') {
                                      <div class="preview-input flex items-center gap-2">
                                        <i class="pi pi-at text-surface-400 text-xs"></i>
                                        <span>email@example.com</span>
                                      </div>
                                    }
                                    @case ('phone') {
                                      <div class="preview-input flex items-center gap-2">
                                        <i class="pi pi-phone text-surface-400 text-xs"></i>
                                        <span>(555) 123-4567</span>
                                      </div>
                                    }
                                    @case ('textarea') {
                                      <div class="preview-input" style="height: 4rem; display: flex; align-items: flex-start; padding-top: 0.5rem;">Sample longer text...</div>
                                    }
                                    @case ('date') {
                                      <div class="preview-input flex items-center gap-2">
                                        <i class="pi pi-calendar text-surface-400 text-xs"></i>
                                        <span>Select date</span>
                                      </div>
                                    }
                                    @case ('datetime') {
                                      <div class="preview-input flex items-center gap-2">
                                        <i class="pi pi-clock text-surface-400 text-xs"></i>
                                        <span>Select date & time</span>
                                      </div>
                                    }
                                    @case ('select') {
                                      <div class="preview-input flex items-center justify-between">
                                        <span class="text-surface-400">Select an option</span>
                                        <i class="pi pi-chevron-down text-surface-400 text-xs"></i>
                                      </div>
                                    }
                                    @case ('multiselect') {
                                      <div class="preview-input flex items-center justify-between">
                                        <span class="text-surface-400">Select options</span>
                                        <i class="pi pi-chevron-down text-surface-400 text-xs"></i>
                                      </div>
                                    }
                                    @case ('checkbox') {
                                      <div class="space-y-1.5">
                                        @for (opt of (question.options?.slice(0,3) || ['Option 1', 'Option 2']); track opt) {
                                          <label class="flex items-center gap-2 text-xs text-surface-500">
                                            <span class="w-3.5 h-3.5 rounded border border-surface-300 inline-block"></span>
                                            {{ opt }}
                                          </label>
                                        }
                                        @if (!question.options?.length) {
                                          <span class="text-xs text-surface-400 italic">No options yet</span>
                                        }
                                      </div>
                                    }
                                    @case ('radio') {
                                      <div class="space-y-1.5">
                                        @for (opt of (question.options?.slice(0,3) || ['Option 1', 'Option 2']); track opt) {
                                          <label class="flex items-center gap-2 text-xs text-surface-500">
                                            <span class="w-3.5 h-3.5 rounded-full border border-surface-300 inline-block"></span>
                                            {{ opt }}
                                          </label>
                                        }
                                        @if (!question.options?.length) {
                                          <span class="text-xs text-surface-400 italic">No options yet</span>
                                        }
                                      </div>
                                    }
                                    @case ('boolean') {
                                      <div class="flex items-center gap-2">
                                        <div class="w-9 h-5 rounded-full bg-surface-200 relative flex-shrink-0">
                                          <div class="w-3.5 h-3.5 rounded-full bg-white absolute top-0.5 left-0.5 shadow-xs"></div>
                                        </div>
                                        <span class="text-xs text-surface-400">Toggle</span>
                                      </div>
                                    }
                                    @case ('file') {
                                      <div class="preview-upload-zone">
                                        <i class="pi pi-upload text-surface-400 text-sm"></i>
                                        <span class="text-xs text-surface-400">Click or drag to upload</span>
                                      </div>
                                    }
                                    @case ('fileupload') {
                                      <div class="preview-upload-zone">
                                        <i class="pi pi-upload text-surface-400 text-sm"></i>
                                        <span class="text-xs text-surface-400">Click or drag to upload</span>
                                      </div>
                                    }
                                    @case ('painpoint') {
                                      <div class="preview-input flex items-center gap-2">
                                        <i class="pi pi-map-marker text-surface-400 text-xs"></i>
                                        <span class="text-surface-400">Pain point: intensity 5/10</span>
                                      </div>
                                    }
                                    @case ('bodyselector') {
                                      <div class="preview-upload-zone">
                                        <i class="pi pi-user text-indigo-400 text-sm"></i>
                                        <span class="text-xs text-surface-500">Interactive body map</span>
                                      </div>
                                    }
                                    @case ('painscale') {
                                      <div class="flex items-center gap-1.5">
                                        @for (i of [1,2,3,4,5,6,7,8,9,10]; track i) {
                                          <div class="w-5 h-5 rounded border border-surface-200 bg-surface-100 flex items-center justify-center"
                                               [style.background]="i <= 3 ? '#f0fdf4' : i <= 6 ? '#fffbeb' : '#fff1f2'"
                                               [style.borderColor]="i <= 3 ? '#bbf7d0' : i <= 6 ? '#fed7aa' : '#fecdd3'">
                                            <span class="text-xs leading-none" style="font-size: 9px;">{{ i }}</span>
                                          </div>
                                        }
                                      </div>
                                    }
                                    @default {
                                      <div class="preview-input">Input</div>
                                    }
                                  }
                                </div>

                                @if (question.validationRules?.length) {
                                  <div class="flex items-center gap-1 mt-2">
                                    <i class="pi pi-shield text-xs text-blue-500"></i>
                                    <span class="text-xs text-blue-600">{{ question.validationRules!.length }} validation rule{{ question.validationRules!.length > 1 ? 's' : '' }}</span>
                                  </div>
                                }
                              </div>
                            }

                            @if (group.questions.length === 0) {
                              <div class="text-center py-4 border-2 border-dashed border-surface-200 rounded-lg">
                                <p class="text-xs text-surface-400 italic">No questions yet</p>
                                <button
                                  class="text-xs text-primary-500 hover:text-primary-700 mt-1"
                                  (click)="addQuestion(getSectionId(group)!, group.groupId); $event.stopPropagation()">
                                  + Add question
                                </button>
                              </div>
                            }
                          </div>
                        </div>
                      }

                      @if (section.groups.length === 0) {
                        <div class="text-center py-6 border-2 border-dashed border-surface-200 rounded-xl">
                          <p class="text-sm text-surface-400 italic">No groups yet</p>
                        </div>
                      }
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        </div>

        <!-- Right Property Panel -->
        <div class="builder-sidebar-right">
          <div class="sidebar-section-header">
            <i class="pi pi-sliders-h text-xs text-surface-400"></i>
            <span>Properties</span>
          </div>

          <div class="properties-content">
            <!-- No selection state -->
            @if (!selectedMode() || selectedMode() === null) {
              <div class="properties-empty-state">
                <i class="pi pi-mouse-pointer"></i>
                <p>Select an element to edit its properties</p>
              </div>
            }

            <!-- Schema Properties -->
            @if (selectedMode() === 'schema') {
              <div class="space-y-4 animate-fade-in">
                <div class="prop-group">
                  <label class="prop-label">Schema Name <span class="text-red-400">*</span></label>
                  <input
                    type="text"
                    pInputText
                    [(ngModel)]="schemaName"
                    placeholder="e.g. Pre-Visit Intake Form"
                    class="w-full" />
                </div>

                <div class="prop-group">
                  <label class="prop-label">Description</label>
                  <textarea
                    pTextarea
                    [(ngModel)]="schemaDescription"
                    placeholder="Describe the purpose of this form..."
                    rows="3"
                    class="w-full">
                  </textarea>
                </div>

                <div class="prop-group">
                  <div class="flex items-center gap-2">
                    <p-checkbox
                      [(ngModel)]="isDefault"
                      [binary]="true"
                      inputId="isDefault">
                    </p-checkbox>
                    <label for="isDefault" class="prop-label mb-0 cursor-pointer">Set as default schema</label>
                  </div>
                  <p class="text-xs text-surface-400 mt-1 ml-6">Default schema is used for new patient intakes</p>
                </div>

                <div class="prop-info-box">
                  <i class="pi pi-info-circle text-xs"></i>
                  <span>Schema version: <strong>{{ schemaVersion() }}</strong></span>
                </div>
              </div>
            }

            <!-- Section Properties -->
            @if (selectedMode() === 'section' && selectedSection()) {
              <div class="space-y-4 animate-fade-in">
                <div class="prop-section-type-badge">
                  <i class="pi pi-folder text-amber-500 text-xs"></i>
                  <span>Section</span>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Title <span class="text-red-400">*</span></label>
                  <input
                    type="text"
                    pInputText
                    [(ngModel)]="selectedSection()!.title"
                    placeholder="Section title"
                    class="w-full" />
                </div>

                <div class="prop-group">
                  <label class="prop-label">Description</label>
                  <textarea
                    pTextarea
                    [(ngModel)]="selectedSection()!.description"
                    placeholder="Optional description"
                    rows="2"
                    class="w-full">
                  </textarea>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Order</label>
                  <p-inputNumber
                    [(ngModel)]="selectedSection()!.order"
                    [min]="1"
                    [showButtons]="true"
                    class="w-full">
                  </p-inputNumber>
                </div>
              </div>
            }

            <!-- Group Properties -->
            @if (selectedMode() === 'group' && selectedGroup()) {
              <div class="space-y-4 animate-fade-in">
                <div class="prop-section-type-badge" style="background: #eff6ff; color: #3b82f6; border-color: #bfdbfe;">
                  <i class="pi pi-th-large text-blue-500 text-xs"></i>
                  <span>Group</span>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Title <span class="text-red-400">*</span></label>
                  <input
                    type="text"
                    pInputText
                    [(ngModel)]="selectedGroup()!.title"
                    placeholder="Group title"
                    class="w-full" />
                </div>

                <div class="prop-group">
                  <label class="prop-label">Description</label>
                  <textarea
                    pTextarea
                    [(ngModel)]="selectedGroup()!.description"
                    placeholder="Optional description"
                    rows="2"
                    class="w-full">
                  </textarea>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Order</label>
                  <p-inputNumber
                    [(ngModel)]="selectedGroup()!.order"
                    [min]="1"
                    [showButtons]="true"
                    class="w-full">
                  </p-inputNumber>
                </div>
              </div>
            }

            <!-- Question Properties -->
            @if (selectedMode() === 'question' && selectedQuestion()) {
              <div class="space-y-4 animate-fade-in">
                <div class="prop-section-type-badge" style="background: #f0fdf4; color: #16a34a; border-color: #bbf7d0;">
                  <i class="pi pi-question-circle text-green-600 text-xs"></i>
                  <span>Question</span>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Question Text <span class="text-red-400">*</span></label>
                  <textarea
                    pTextarea
                    [(ngModel)]="selectedQuestion()!.text"
                    placeholder="Enter your question"
                    rows="2"
                    class="w-full">
                  </textarea>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Question Type <span class="text-red-400">*</span></label>
                  <p-select
                    [(ngModel)]="selectedQuestion()!.type"
                    [options]="questionTypes"
                    optionLabel="label"
                    optionValue="value"
                    placeholder="Select type"
                    class="w-full">
                    <ng-template let-option pTemplate="item">
                      <div class="flex items-center gap-2">
                        <i [class]="option.icon + ' text-surface-500'"></i>
                        <span>{{ option.label }}</span>
                      </div>
                    </ng-template>
                  </p-select>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Placeholder</label>
                  <input
                    type="text"
                    pInputText
                    [(ngModel)]="selectedQuestion()!.placeholder"
                    placeholder="Placeholder text"
                    class="w-full" />
                </div>

                <div class="prop-group">
                  <label class="prop-label">Helper Text</label>
                  <textarea
                    pTextarea
                    [(ngModel)]="selectedQuestion()!.description"
                    placeholder="Additional guidance for the patient"
                    rows="2"
                    class="w-full">
                  </textarea>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Help Text</label>
                  <textarea
                    pTextarea
                    [(ngModel)]="selectedQuestion()!.helpText"
                    placeholder="Detailed help shown below the input"
                    rows="2"
                    class="w-full">
                  </textarea>
                </div>

                <div class="flex items-center gap-2 p-3 rounded-lg bg-surface-50 border border-surface-200">
                  <p-checkbox
                    [(ngModel)]="selectedQuestion()!.required"
                    [binary]="true"
                    inputId="required">
                  </p-checkbox>
                  <label for="required" class="prop-label mb-0 cursor-pointer">Required field</label>
                </div>

                <div class="prop-group">
                  <label class="prop-label">Order</label>
                  <p-inputNumber
                    [(ngModel)]="selectedQuestion()!.order"
                    [min]="1"
                    [showButtons]="true"
                    class="w-full">
                  </p-inputNumber>
                </div>

                @if (selectedQuestion()!.type === 'select' || selectedQuestion()!.type === 'radio' || selectedQuestion()!.type === 'checkbox' || selectedQuestion()!.type === 'multiselect') {
                  <div class="prop-group">
                    <label class="prop-label">Options <span class="text-xs text-surface-400 font-normal">(comma-separated)</span></label>
                    <textarea
                      pTextarea
                      [ngModel]="getOptionsString()"
                      (ngModelChange)="updateOptions($event)"
                      placeholder="Option A, Option B, Option C"
                      rows="3"
                      class="w-full">
                    </textarea>
                  </div>
                }

                <!-- Divider -->
                <div class="prop-divider"></div>

                <!-- Validation Rules -->
                <div>
                  <div class="prop-sub-header">
                    <div class="flex items-center gap-2">
                      <i class="pi pi-shield text-blue-500 text-xs"></i>
                      <span class="prop-sub-title">Validation Rules</span>
                      @if (getValidationRules().length) {
                        <span class="prop-count-badge">{{ getValidationRules().length }}</span>
                      }
                    </div>
                    <p-button
                      icon="pi pi-plus"
                      size="small"
                      [text]="true"
                      [rounded]="true"
                      severity="info"
                      (onClick)="addValidationRule()"
                      pTooltip="Add rule">
                    </p-button>
                  </div>

                  @for (rule of getValidationRules(); track $index) {
                    <div class="rule-card">
                      <div class="flex gap-2 mb-2">
                        <p-select
                          [(ngModel)]="rule.ruleType"
                          [options]="engine.validationRuleTypes"
                          optionLabel="label"
                          optionValue="value"
                          placeholder="Rule type"
                          class="flex-1">
                        </p-select>
                        <p-button
                          icon="pi pi-trash"
                          size="small"
                          [text]="true"
                          [rounded]="true"
                          severity="danger"
                          (onClick)="removeValidationRule($index)">
                        </p-button>
                      </div>

                      @if (rule.ruleType === 'pattern') {
                        <div>
                          <label class="rule-label">Pattern</label>
                          <input pInputText [(ngModel)]="rule.value" placeholder="^[a-zA-Z]+$" class="w-full font-mono text-sm" />
                        </div>
                      } @else if (rule.ruleType === 'min' || rule.ruleType === 'max') {
                        <div>
                          <label class="rule-label">Value</label>
                          <p-inputNumber [(ngModel)]="rule.value" [showButtons]="true" class="w-full"></p-inputNumber>
                        </div>
                      } @else if (rule.ruleType === 'minLength' || rule.ruleType === 'maxLength') {
                        <div>
                          <label class="rule-label">Characters</label>
                          <p-inputNumber [(ngModel)]="rule.value" [min]="0" [showButtons]="true" class="w-full"></p-inputNumber>
                        </div>
                      }

                      @if (rule.ruleType !== 'required' && rule.ruleType !== 'email' && rule.ruleType !== 'url') {
                        <div class="mt-2">
                          <label class="rule-label">Error message</label>
                          <input pInputText [(ngModel)]="rule.message" placeholder="Custom error message" class="w-full text-sm" />
                        </div>
                      }
                    </div>
                  } @empty {
                    <div class="rule-empty">
                      <i class="pi pi-shield text-surface-300 text-base"></i>
                      <p>No validation rules</p>
                      <button class="text-xs text-primary-500 hover:text-primary-700" (click)="addValidationRule()">+ Add rule</button>
                    </div>
                  }
                </div>

                <!-- Divider -->
                <div class="prop-divider"></div>

                <!-- Conditional Logic -->
                <div>
                  <div class="prop-sub-header">
                    <div class="flex items-center gap-2">
                      <i class="pi pi-sliders-h text-orange-500 text-xs"></i>
                      <span class="prop-sub-title">Conditional Logic</span>
                      @if (getConditions().length) {
                        <span class="prop-count-badge" style="background: #fff7ed; color: #c2410c; border-color: #fed7aa;">{{ getConditions().length }}</span>
                      }
                    </div>
                    <p-button
                      icon="pi pi-plus"
                      size="small"
                      [text]="true"
                      [rounded]="true"
                      severity="info"
                      (onClick)="addCondition()"
                      pTooltip="Add condition">
                    </p-button>
                  </div>

                  @if (getConditions().length > 1) {
                    <div class="mb-3 flex items-center gap-2">
                      <span class="text-xs text-surface-500">Match:</span>
                      <p-selectbutton
                        [(ngModel)]="conditionLogic"
                        [options]="conditionLogicOptions"
                        optionLabel="label"
                        optionValue="value"
                        size="small">
                      </p-selectbutton>
                    </div>
                  }

                  @for (condition of getConditions(); track $index) {
                    @if ($index > 0) {
                      <div class="condition-logic-separator">
                        <div class="condition-logic-line"></div>
                         <span class="condition-logic-badge"
                               [class.condition-logic-and]="conditionLogic === 'and'"
                               [class.condition-logic-or]="conditionLogic === 'or'">
                           {{ (conditionLogic || 'and').toUpperCase() }}
                         </span>
                        <div class="condition-logic-line"></div>
                      </div>
                    }
                    <div class="rule-card">
                      <div class="flex gap-2 mb-2">
                        <p-select
                          [(ngModel)]="condition.targetQuestionId"
                          [options]="availableQuestions()"
                          optionLabel="label"
                          optionValue="value"
                          placeholder="Target question"
                          class="flex-1">
                        </p-select>
                        <p-button
                          icon="pi pi-trash"
                          size="small"
                          [text]="true"
                          [rounded]="true"
                          severity="danger"
                          (onClick)="removeCondition($index)">
                        </p-button>
                      </div>
                      <div class="flex gap-2">
                        <p-select
                          [(ngModel)]="condition.operator"
                          [options]="engine.conditionOperators"
                          optionLabel="label"
                          optionValue="value"
                          placeholder="Operator"
                          class="flex-1">
                        </p-select>
                        <input
                          pInputText
                          [(ngModel)]="condition.value"
                          placeholder="Value"
                          class="flex-1" />
                      </div>
                    </div>
                  } @empty {
                    <div class="rule-empty">
                      <i class="pi pi-sliders-h text-surface-300 text-base"></i>
                      <p>No conditions</p>
                      <button class="text-xs text-primary-500 hover:text-primary-700" (click)="addCondition()">+ Add condition</button>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrl: './schema-builder.component.css'
})
export class SchemaBuilderComponent implements OnInit {
  private readonly apiService = inject(IntakeApiService);
  protected readonly engine = inject(DynamicFormEngineService);
  protected readonly snackbar = inject(SnackbarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Signals
  selectedSchema = signal<FormSchemaResponse | null>(null);
  loading = signal(false);
  saving = signal(false);
  publishing = signal(false);
  selectedItem = signal<string>('schema');
  selectedMode = signal<BuilderMode>('schema');
  schemaVersion = signal(1);

  // Form Schema Signal
  formSchema = signal<DynamicFormSchemaDto>({
    schemaVersion: 1,
    sections: []
  });

  // Selected items
  selectedSection = signal<FormSectionDto | null>(null);
  selectedGroup = signal<FormGroupDto | null>(null);
  selectedQuestion = signal<FormQuestionDto | null>(null);

  // Computed
  availableQuestions = computed(() => {
    const current = this.selectedQuestion();
    return this.engine.getAllQuestions(this.formSchema())
      .filter(q => !current || q.questionId !== current.questionId)
      .map(q => ({ label: q.text, value: q.questionId }));
  });

  // Expansion state
  private expandedSections = signal<Set<string>>(new Set());
  private expandedGroups = signal<Set<string>>(new Set());

  // Form data
  schemaName = '';
  schemaDescription = '';
  isDefault = false;

  // Condition logic
  conditionLogic: 'and' | 'or' = 'and';
  readonly conditionLogicOptions = [
    { label: 'All (AND)', value: 'and' },
    { label: 'Any (OR)', value: 'or' }
  ];

  // Question types
  questionTypes: QuestionTypeOption[] = [
    { label: 'Text', value: 'text', icon: 'pi pi-pencil' },
    { label: 'Number', value: 'number', icon: 'pi pi-hashtag' },
    { label: 'Email', value: 'email', icon: 'pi pi-at' },
    { label: 'Phone', value: 'phone', icon: 'pi pi-phone' },
    { label: 'Date', value: 'date', icon: 'pi pi-calendar' },
    { label: 'Date Time', value: 'datetime', icon: 'pi pi-clock' },
    { label: 'Textarea', value: 'textarea', icon: 'pi pi-align-left' },
    { label: 'Dropdown', value: 'select', icon: 'pi pi-list' },
    { label: 'Multi Select', value: 'multiselect', icon: 'pi pi-list' },
    { label: 'Checkbox', value: 'checkbox', icon: 'pi pi-check-square' },
    { label: 'Radio', value: 'radio', icon: 'pi pi-circle' },
    { label: 'Boolean', value: 'boolean', icon: 'pi pi-check' },
    { label: 'File Upload', value: 'file', icon: 'pi pi-upload' },
    { label: 'File Upload (Legacy)', value: 'fileupload', icon: 'pi pi-upload' },
    { label: 'Pain Point', value: 'painpoint', icon: 'pi pi-map-marker' },
    { label: 'Body Selector', value: 'bodyselector', icon: 'pi pi-user' },
    { label: 'Pain Scale', value: 'painscale', icon: 'pi pi-chart-bar' }
  ];

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const schemaId = params.get('id');
      if (schemaId) {
        this.loadSchema(schemaId);
      } else {
        this.resetBuilder();
      }
    });
  }

  private resetBuilder(): void {
    this.selectedSchema.set(null);
    this.schemaName = '';
    this.schemaDescription = '';
    this.isDefault = false;
    this.schemaVersion.set(1);
    this.formSchema.set({ schemaVersion: 1, sections: [] });
  }

  isEditMode(): boolean {
    return !!this.selectedSchema();
  }

  loadSchema(id: string): void {
    this.loading.set(true);
    this.apiService.getFormSchemaById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (schema) => {
        this.selectedSchema.set(schema);
        this.schemaName = schema.name;
        this.schemaDescription = schema.description || '';
        this.isDefault = schema.isDefault;
        this.schemaVersion.set(schema.version);

        try {
          const parsed = JSON.parse(schema.schemaJson) as DynamicFormSchemaDto;
          this.formSchema.set({
            schemaVersion: parsed.schemaVersion ?? 1,
            sections: parsed.sections ?? []
          });
        } catch (error) {
          console.error('Failed to parse schema JSON:', error);
          this.formSchema.set({ schemaVersion: 1, sections: [] });
        }

        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.snackbar.error('Failed to load schema', [this.extractError(err)]);
      }
    });
  }

  selectItem(type: BuilderMode, item: any): void {
    this.selectedMode.set(type);

    if (type === 'schema') {
      this.selectedItem.set('schema');
      this.selectedSection.set(null);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(null);
    } else if (type === 'section') {
      this.selectedItem.set(item.sectionId);
      this.selectedSection.set(item);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(null);
      this.toggleSection(item.sectionId);
    } else if (type === 'group') {
      this.selectedItem.set(item.groupId);
      this.selectedSection.set(null);
      this.selectedGroup.set(item);
      this.selectedQuestion.set(null);
      this.toggleGroup(item.groupId);
    } else if (type === 'question') {
      this.selectedItem.set(item.questionId);
      this.selectedSection.set(null);
      this.selectedGroup.set(null);
      this.selectedQuestion.set(item);
    }
  }

  // Expansion management
  isSectionExpanded(sectionId: string): boolean {
    return this.expandedSections().has(sectionId);
  }

  isGroupExpanded(groupId: string): boolean {
    return this.expandedGroups().has(groupId);
  }

  toggleSection(sectionId: string): void {
    const expanded = new Set(this.expandedSections());
    if (expanded.has(sectionId)) {
      expanded.delete(sectionId);
    } else {
      expanded.add(sectionId);
    }
    this.expandedSections.set(expanded);
  }

  toggleGroup(groupId: string): void {
    const expanded = new Set(this.expandedGroups());
    if (expanded.has(groupId)) {
      expanded.delete(groupId);
    } else {
      expanded.add(groupId);
    }
    this.expandedGroups.set(expanded);
  }

  // Helper to find sectionId by groupId
  getSectionId(group: FormGroupDto): string | undefined {
    return this.formSchema().sections.find(s => s.groups.some(g => g.groupId === group.groupId))?.sectionId;
  }

  getQuestionTypeIcon(type: string): string {
    return this.questionTypes.find(t => t.value === type)?.icon || 'pi pi-question';
  }

  // CRUD operations
  addSection(): void {
    const schema = this.formSchema();
    const newSection: FormSectionDto = {
      sectionId: this.generateId('section'),
      title: 'New Section',
      description: '',
      order: schema.sections.length + 1,
      groups: []
    };

    this.formSchema.set({
      ...schema,
      sections: [...schema.sections, newSection]
    });

    this.toggleSection(newSection.sectionId);
    this.selectItem('section', newSection);
  }

  deleteSection(sectionId: string): void {
    const schema = this.formSchema();
    this.formSchema.set({
      ...schema,
      sections: schema.sections.filter(s => s.sectionId !== sectionId)
    });

    if (this.selectedItem() === sectionId) {
      this.selectItem('schema', null);
    }
  }

  addGroup(sectionId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);

    if (section) {
      const newGroup: FormGroupDto = {
        groupId: this.generateId('group'),
        title: 'New Group',
        description: '',
        order: section.groups.length + 1,
        questions: []
      };

      section.groups.push(newGroup);
      this.formSchema.set({ ...schema });

      this.toggleGroup(newGroup.groupId);
      this.selectItem('group', newGroup);
    }
  }

  deleteGroup(sectionId: string, groupId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);

    if (section) {
      section.groups = section.groups.filter(g => g.groupId !== groupId);
      this.formSchema.set({ ...schema });

      if (this.selectedItem() === groupId) {
        this.selectItem('schema', null);
      }
    }
  }

  addQuestion(sectionId: string, groupId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);
    const group = section?.groups.find(g => g.groupId === groupId);

    if (group) {
      const newQuestion: FormQuestionDto = {
        questionId: this.generateId('question'),
        text: 'New Question',
        description: '',
        type: 'text',
        order: group.questions.length + 1,
        required: false,
        options: []
      };

      group.questions.push(newQuestion);
      this.formSchema.set({ ...schema });

      this.selectItem('question', newQuestion);
    }
  }

  deleteQuestion(sectionId: string, groupId: string, questionId: string): void {
    const schema = this.formSchema();
    const section = schema.sections.find(s => s.sectionId === sectionId);
    const group = section?.groups.find(g => g.groupId === groupId);

    if (group) {
      group.questions = group.questions.filter(q => q.questionId !== questionId);
      this.formSchema.set({ ...schema });

      if (this.selectedItem() === questionId) {
        this.selectItem('schema', null);
      }
    }
  }

  generateId(prefix: string): string {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  getOptionsString(): string {
    return this.selectedQuestion()?.options?.join(', ') || '';
  }

  updateOptions(value: string): void {
    const question = this.selectedQuestion();
    if (question) {
      question.options = value.split(',').map(o => o.trim()).filter(o => o.length > 0);
    }
  }

  getConditions(): QuestionConditionDto[] {
    return this.selectedQuestion()?.conditions || [];
  }

  getValidationRules(): ValidationRuleDto[] {
    return this.selectedQuestion()?.validationRules || [];
  }

  addCondition(): void {
    const question = this.selectedQuestion();
    if (!question) return;
    if (!question.conditions) {
      question.conditions = [];
    }
    question.conditions.push({
      targetQuestionId: '',
      operator: 'equals',
      value: ''
    });
    this.formSchema.set({ ...this.formSchema() });
  }

  removeCondition(index: number): void {
    const question = this.selectedQuestion();
    if (!question?.conditions) return;
    question.conditions.splice(index, 1);
    if (question.conditions.length === 0) {
      question.conditions = undefined;
    }
    this.formSchema.set({ ...this.formSchema() });
  }

  addValidationRule(): void {
    const question = this.selectedQuestion();
    if (!question) return;
    if (!question.validationRules) {
      question.validationRules = [];
    }
    question.validationRules.push({
      ruleType: 'required',
      value: undefined,
      message: undefined
    });
    this.formSchema.set({ ...this.formSchema() });
  }

  removeValidationRule(index: number): void {
    const question = this.selectedQuestion();
    if (!question?.validationRules) return;
    question.validationRules.splice(index, 1);
    if (question.validationRules.length === 0) {
      question.validationRules = undefined;
    }
    this.formSchema.set({ ...this.formSchema() });
  }

  private getCurrentSchemaJson(): string {
    return this.engine.serializeSchema(this.formSchema());
  }

  saveDraft(): void {
    this.saving.set(true);
    const schemaJson = this.getCurrentSchemaJson();
    const existing = this.selectedSchema();

    const request = {
      name: this.schemaName,
      description: this.schemaDescription || undefined,
      schemaJson,
      isDefault: this.isDefault
    };

    if (existing) {
      this.apiService.updateFormSchema(existing.id, request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (updated) => {
          this.selectedSchema.set(updated);
          this.schemaVersion.set(updated.version);
          this.saving.set(false);
          this.snackbar.success('Schema saved', ['Draft updated successfully']);
        },
        error: (err: any) => {
          this.saving.set(false);
          this.snackbar.error('Save failed', [this.extractError(err)]);
        }
      });
    } else {
      this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (created) => {
          this.selectedSchema.set(created);
          this.schemaVersion.set(created.version);
          this.saving.set(false);
          this.snackbar.success('Schema saved', ['Draft created successfully']);
          this.router.navigate(['app/intake/schemas/edit', created.id], { replaceUrl: true });
        },
        error: (err: any) => {
          this.saving.set(false);
          this.snackbar.error('Save failed', [this.extractError(err)]);
        }
      });
    }
  }

  publish(): void {
    const existing = this.selectedSchema();
    if (!existing) {
      this.saveDraftWithCallback(() => this.doPublish());
    } else {
      this.doPublish();
    }
  }

  private extractError(err: any): string {
    const body = err?.error;
    if (body?.detail) return body.detail;
    if (body?.errors) {
      const msgs = Object.values(body.errors as Record<string, string[]>).flat();
      return msgs.join('; ');
    }
    return body?.title || 'Unexpected error';
  }

  private saveDraftWithCallback(callback: () => void): void {
    this.saving.set(true);
    const schemaJson = this.getCurrentSchemaJson();
    const request = {
      name: this.schemaName,
      description: this.schemaDescription || undefined,
      schemaJson,
      isDefault: this.isDefault
    };

    this.apiService.createFormSchema(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (created) => {
        this.selectedSchema.set(created);
        this.schemaVersion.set(created.version);
        this.saving.set(false);
        callback();
      },
      error: (err: any) => {
        this.saving.set(false);
        this.snackbar.error('Save failed', [this.extractError(err)]);
      }
    });
  }

  private doPublish(): void {
    const existing = this.selectedSchema();
    if (!existing) return;

    this.publishing.set(true);
    this.apiService.publishFormSchema(existing.id, { version: existing.version }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (published) => {
        this.selectedSchema.set(published);
        this.schemaVersion.set(published.version);
        this.publishing.set(false);
        this.snackbar.success('Schema published', ['Form schema is now live']);
        this.router.navigate(['app/intake/schemas']);
      },
      error: (err: any) => {
        this.publishing.set(false);
        this.snackbar.error('Publish failed', [this.extractError(err)]);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['app/intake/schemas']);
  }
}

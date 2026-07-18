import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, ValidationErrors, Validators, AbstractControl } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DialogModule } from 'primeng/dialog';
import { ProgressBarModule } from 'primeng/progressbar';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { UserProfile } from '../../Shared/Models/account.model';
import { AccountService } from '../../Core/Services/account.service';
import { ImageCropperComponent, ImageCroppedEvent } from 'ngx-image-cropper';
import { AuthService } from '../../Core/Services/auth.service';
import { Router } from '@angular/router';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirmPassword = group.get('confirmPassword')?.value;
  return newPassword === confirmPassword ? null : { passwordsMismatch: true };
}

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    DialogModule,
    ProgressBarModule,
    TextareaModule,
    InputNumberModule,
    ImageCropperComponent
],
  templateUrl: './account.component.html',
  styleUrl: './account.component.css',
})
export class AccountComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly fb = inject(FormBuilder);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  profile = signal<UserProfile | null>(null);
  isEditMode = signal(false);
  isSaving = signal(false);
  photoPreviewUrl = signal<string | null>(null);
  selectedPhotoFile: File | null = null;
  removePhotoFlag = signal(false);

  isPasswordDialogOpen = signal(false);
  isChangingPassword = signal(false);

  isCropperOpen = signal(false);
  imageToCrop = signal<string | null>(null);
  private croppedBlob: Blob | null = null;

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      this.imageToCrop.set(reader.result as string);
      this.isCropperOpen.set(true);
    };
    reader.readAsDataURL(file);

    input.value = ''; // allow re-selecting the same file later
  }

  onImageCropped(event: ImageCroppedEvent): void {
    if (event.blob) {
      this.croppedBlob = event.blob;
    }
  }

  goToStaff(): void {
    this.router.navigateByUrl('/app/account/staff');
  }

  confirmCrop(): void {
    if (!this.croppedBlob) return;

    const file = new File([this.croppedBlob], 'profile-photo.png', { type: 'image/png' });
    this.selectedPhotoFile = file;
    this.removePhotoFlag.set(false);
    this.photoPreviewUrl.set(URL.createObjectURL(this.croppedBlob));

    this.isCropperOpen.set(false);
    this.imageToCrop.set(null);
    this.croppedBlob = null;
  }

  cancelCrop(): void {
    this.isCropperOpen.set(false);
    this.imageToCrop.set(null);
    this.croppedBlob = null;
  }


  form = this.fb.group({
    userName: ['', [Validators.required]],
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    phoneNumber: [''],
    title: [''],
    clinicName: [''],
    clinicAddress: [''],
    about: [''],
    yearsOfExperience: [null as number | null],
  });

  passwordForm = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: passwordsMatchValidator });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.accountService.getProfile().subscribe(profile => {
      this.profile.set(profile);
      this.patchForm(profile);
    });
  }

  private patchForm(profile: UserProfile): void {
    this.form.patchValue({
      userName: profile.userName,
      firstName: profile.firstName,
      lastName: profile.lastName,
      phoneNumber: profile.phoneNumber,
      title: profile.title,
      clinicName: profile.clinicName,
      clinicAddress: profile.clinicAddress,
      about: profile.about,
      yearsOfExperience: profile.yearsOfExperience,
    });
    this.photoPreviewUrl.set(profile.profilePictureUrl ?? null);
  }

  enterEditMode(): void {
    this.isEditMode.set(true);
  }

  cancelEdit(): void {
    const current = this.profile();
    if (current) this.patchForm(current);
    this.selectedPhotoFile = null;
    this.removePhotoFlag.set(false);
    this.isEditMode.set(false);
  }


  removePhoto(): void {
    this.selectedPhotoFile = null;
    this.removePhotoFlag.set(true);
    this.photoPreviewUrl.set(null);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const value = this.form.getRawValue();

    this.accountService.updateProfile({
      userName: value.userName!,
      firstName: value.firstName!,
      lastName: value.lastName!,
      phoneNumber: value.phoneNumber,
      title: value.title,
      clinicName: value.clinicName,
      clinicAddress: value.clinicAddress,
      about: value.about,
      yearsOfExperience: value.yearsOfExperience,
      profilePhoto: this.selectedPhotoFile,
      removeProfilePhoto: this.removePhotoFlag(),
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.isEditMode.set(false);
        this.selectedPhotoFile = null;
        this.removePhotoFlag.set(false);
        this.loadProfile();
      },
      error: () => {
        this.isSaving.set(false);
      },
    });
  }

  openPasswordDialog(): void {
    this.passwordForm.reset();
    this.isPasswordDialogOpen.set(true);
  }

  closePasswordDialog(): void {
    this.isPasswordDialogOpen.set(false);
  }

  submitPasswordChange(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isChangingPassword.set(true);
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.accountService.changePassword({
      currentPassword: currentPassword!,
      newPassword: newPassword!,
    }).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.isPasswordDialogOpen.set(false);
        // Optional: snackbar success message here if you have one injected
      },
      error: () => {
        this.isChangingPassword.set(false);
        // errorInterceptor already shows a snackbar for the failure (e.g. wrong current password)
      },
    });
  }

  get initials(): string {
    const p = this.profile();
    if (!p) return '';
    return `${p.firstName?.[0] ?? ''}${p.lastName?.[0] ?? ''}`.toUpperCase();
  }
}
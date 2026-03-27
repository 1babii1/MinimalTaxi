import { ProfileSections } from "@/features/profile/ui/profile-sections";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";

export function ProfilePage() {
  return (
    <AppLayout>
      <ProfileSections />
    </AppLayout>
  );
}

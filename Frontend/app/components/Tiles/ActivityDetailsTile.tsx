import { Calendar, Clock, MapPin, Users, Image as ImageIcon } from 'lucide-react';
import { deleteApiEnrollmentsByActivityIdByMemberId, getApiActivitiesByIdPoster, postApiEnrollments, type Activity, type ActivityResponseDto } from '~/api';
import Tile from './Tile';
import { useKeycloak } from '@react-keycloak/web';
import { useEffect, useState } from 'react';
import Button from '../UI/Button';
import { t } from 'i18next';
import { formatDate } from '~/util/date.util';
import ReactMarkdown from 'react-markdown';
import { isInGroupWithId } from '~/util/group.util';
import remarkGfm from 'remark-gfm';
import { formatForGoogleCalendar, formatForWhatsApp } from '~/util/markdown.util';

export default function ActivityDetailsTile({ activity, setActivity }: { activity: ActivityResponseDto; setActivity?: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>> }) {
  const { keycloak, initialized } = useKeycloak();

  const [submitting, setSubmitting] = useState(false);
  const [posterStatus, setPosterStatus] = useState<"loading" | "loaded" | "error">("loading");

  const posterUrl = `${import.meta.env.ApiUrl}/api/activities/${activity.id}/poster`;
  const isPdf = activity.posterFileName?.toLowerCase().endsWith(".pdf");
  const hasPoster = !!activity.posterFileName;

  const startDate = activity.dateTimeStart ? new Date(activity.dateTimeStart).toLocaleDateString('nl-NL', {
    day: 'numeric', month: 'long', year: 'numeric'
  }) : 'TBA';

  const startTime = activity.dateTimeStart ? new Date(activity.dateTimeStart).toLocaleTimeString('nl-NL', {
    hour: '2-digit', minute: '2-digit'
  }) : 'TBA';

  const endTime = activity.dateTimeEnd ? new Date(activity.dateTimeEnd).toLocaleTimeString('nl-NL', {
    hour: '2-digit', minute: '2-digit'
  }) : 'TBA';

  const isEnrolled = activity.enrollments?.some(e => e.member.id === keycloak.tokenParsed?.UserId) ?? false;

  const handleAddToCalendar = () => {
    const title = encodeURIComponent(activity.name || 'Activiteit');
    const description = encodeURIComponent(formatForGoogleCalendar(activity.dutchDescription) || '');
    const location = encodeURIComponent(activity.location || 'TBA');

    const formatDateGoogle = (dateStr: string | undefined | null) => {
      if (!dateStr) return '';
      return new Date(dateStr).toISOString().replace(/-|:|\.\d+/g, '');
    };

    const startTime = formatDateGoogle(activity.dateTimeStart);
    const endTime = formatDateGoogle(activity.dateTimeEnd);

    const googleUrl = `https://www.google.com/calendar/render?action=TEMPLATE&text=${title}&details=${description}&location=${location}&dates=${startTime}/${endTime}`;

    window.open(googleUrl, '_blank', 'noreferrer');
  };

  const handleEnrollment = async () => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) {
      console.error("User not authenticated or activity missing");
      return;
    }

    try {
      setSubmitting(true);
      
      const response = await postApiEnrollments({
        body: {
          activityId: activity.id,
          memberId: keycloak.tokenParsed.UserId
        }
      });

      if(response.data) {
        const newEnrollment = { isOnWaitingList: response.data.isOnWaitingList, memberId: keycloak.tokenParsed.UserId, activityId: activity.id, member: { id: keycloak.tokenParsed.UserId, firstName: keycloak.tokenParsed?.given_name || '', lastName: keycloak.tokenParsed?.family_name || '' } } as any;
        activity.enrollments = activity.enrollments ? [...activity.enrollments, newEnrollment] : [newEnrollment];
        setActivity && setActivity({ ...activity });
      }

      
    } catch (error) {
      console.error("Error while enrolling:", error);
    } finally {
      setSubmitting(false);
    }
  };

  const handleUnenrollment = async () => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) {
      return;
    }

    try {
      setSubmitting(true);
      
      await deleteApiEnrollmentsByActivityIdByMemberId({
        path: {
          activityId: activity.id,
          memberId: keycloak.tokenParsed.UserId
        }
      });

      activity.enrollments = activity.enrollments?.filter(e => e.member.id !== keycloak.tokenParsed?.UserId);
      setActivity && setActivity({ ...activity });
    } catch (error) {
      console.error("Error while unenrolling:", error);
    } finally {
      setSubmitting(false);
    }
  };

  const handleCopyForWhatsapp = async (lang: "NL" | "EN") => {
    const text = lang === "NL" ?
      `*${activity.name} | ${formatDate(new Date(activity.dateTimeStart || ''), "fullDateTime")} - ${formatDate(new Date(activity.dateTimeEnd || ''), "fullDateTime")} | Locatie: ${activity.location || 'TBA'} | Prijs: ${activity.price === 0 ? 'Gratis' : `€ ${activity.price?.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.dutchDescription)}` :
      `*${activity.name} | ${formatDate(new Date(activity.dateTimeStart || ''), "fullDateTime")} - ${formatDate(new Date(activity.dateTimeEnd || ''), "fullDateTime")} | Location: ${activity.location || 'TBA'} | Price: ${activity.price === 0 ? 'Free' : `€ ${activity.price?.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.englishDescription)}`;

    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      console.error("Copy failed:", err);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
      {/* Poster Column */}
      <div className="lg:col-span-5 lg:sticky lg:top-8">
        <div className="relative w-full aspect-[1/1.414] bg-slate-100 rounded-3xl shadow-lg border border-slate-100 overflow-hidden">

          {/* Loading */}
          {posterStatus === "loading" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)" />
              <p className="text-gray-500 animate-pulse">{t("loading")}</p>
            </div>
          )}

          {/* No poster */}
          {!hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
              <ImageIcon className="text-slate-400 mb-2" size={48} />
              <span className="text-slate-400 text-sm font-medium">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Error fallback */}
          {posterStatus === "error" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
              <ImageIcon className="text-slate-400 mb-2" size={48} />
              <span className="text-slate-400 text-sm font-medium">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Content */}
          {hasPoster && (
            isPdf ? (
              <iframe
                src={`${posterUrl}#toolbar=0&navpanes=0&scrollbar=0`}
                onLoad={() => setPosterStatus("loaded")}
                className="w-full h-full border-none"
                title="Poster PDF"
              />
            ) : (
              <img
                src={posterUrl}
                alt={activity.name ?? ""}
                onLoad={() => setPosterStatus("loaded")}
                onError={() => setPosterStatus("error")}
                className={`w-full h-full object-cover transition-opacity duration-500 ${
                  posterStatus === "loading" ? "opacity-0" : "opacity-100"
                }`}
              />
            )
          )}
        </div>
      </div>

      {/* Info Column */}
      <div className="lg:col-span-7 flex flex-col gap-6">
        <section>
          <h1 className="text-4xl font-black text-slate-900 mt-4 mb-2 tracking-tight">
            {activity.name}
          </h1>
          <p className="text-2xl font-semibold text-slate-800">
            {activity.price === 0 ? 'Gratis' : `€ ${activity.price?.toFixed(2)}`}
          </p>
        </section>

        <Tile className="bg-white border border-slate-200 shadow-sm">
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-bold text-slate-900">{t("description")}</h3>

            {isInGroupWithId(keycloak.tokenParsed, import.meta.env.BOARD_GROUP_ID) && (
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  onClick={() => handleCopyForWhatsapp("NL")}
                  className="text-xs px-3 py-1"
                >
                  {t("copy")} NL
                </Button>

                <Button
                  variant="secondary"
                  onClick={() => handleCopyForWhatsapp("EN")}
                  className="text-xs px-3 py-1"
                >
                  {t("copy")} EN
                </Button>
              </div>
            )}
          </div>

            <div className="prose prose-sm max-w-none
              prose-p:!my-0.5
              prose-ul:!my-0.5
              prose-ol:!my-0.5
              prose-li:!my-0
              [&_li>p]:!my-0
              leading-snug
            ">
              <ReactMarkdown 
                remarkPlugins={[remarkGfm]}
                  components={{
                    a: ({ node, ...props }) => (
                      <a 
                        className="text-(--board-primary) hover:text-(--board-primary-dark) font-semibold underline underline-offset-4" 
                        target="_blank" 
                        rel="noreferrer" 
                        {...props} 
                      />
                    ),
                  }}>
                {keycloak.tokenParsed?.locale == "NL"
                  ? activity.dutchDescription || "Geen beschrijving beschikbaar."
                  : activity.englishDescription || "No description available."}
              </ReactMarkdown>
            </div>
        </Tile>

        {/* Info Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-y-6 gap-x-4 p-2">
          <InfoItem icon={<Calendar size={18}/>} label={t("date")} value={`${startDate} ${startTime}-${endTime}`} />
          <InfoItem icon={<Clock size={18}/>} label={t("unenrollment_deadline")} value={activity.unenrollmentDeadline ? formatDate(new Date(activity.unenrollmentDeadline), "fullDateTime") : t("none")} />
          <InfoItem icon={<MapPin size={18}/>} label={t("location")} value={activity.location || ''} />
          <InfoItem icon={<Users size={18}/>} label={t("participants")} value={`${activity.enrollments?.filter(e => !e.isOnWaitingList).length ?? 0}${activity.participantLimit ? ` ${t("of")} ${activity.participantLimit}` : ''}`} />
        </div>

        {/* Actions */}
        <div className="flex flex-col gap-3 pt-4 border-t border-slate-100">
          {isEnrolled ? (
            <Button variant='primary' onClick={handleUnenrollment} className="bg-red-500 hover:bg-red-600" disabled={submitting || (activity.unenrollmentDeadline ? new Date(Date.now()) > new Date(activity.unenrollmentDeadline) : false)}>
              {t("sign_out")}{submitting && ('...')}
            </Button>
          ) : (
            <Button variant='primary' onClick={handleEnrollment} disabled={submitting}>
              {activity.participantLimit && activity.participantLimit <= (activity.enrollments?.length ?? 0) ? t("sign_in_on_waitlist") : t("sign_in")}{submitting && ('...')}
            </Button>
          )}
          <Button variant='secondary' className="bg-white hover:bg-slate-50 dark:bg-slate-800 dark:hover:bg-slate-700" onClick={handleAddToCalendar}>
            <div className='flex items-center gap-2'><Calendar size={18} />{t("add_to_calendar")}</div>
          </Button>
        </div>
      </div>
    </div>
  );
}

function InfoItem({ icon, label, value }: { icon: React.ReactNode, label: string, value: string }) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-1 p-2 bg-slate-50 rounded-lg text-slate-400 font-bold">{icon}</div>
      <div>
        <p className="text-[10px] uppercase font-bold text-slate-400 tracking-wider leading-none mb-1">{label}</p>
        <p className="text-slate-700 font-semibold leading-tight">{value}</p>
      </div>
    </div>
  );
}
import { Calendar, Clock, MapPin, Users, Image as ImageIcon } from 'lucide-react';
import { deleteApiEnrollmentsByActivityIdByMemberId, postApiEnrollments, putApiEnrollmentsByActivityIdByMemberId, type ActivityResponseDto } from '~/api';
import Tile from './Tile';
import { useKeycloak } from '@react-keycloak/web';
import { useState } from 'react';
import Button from '../UI/Button';
import { t } from 'i18next';
import { formatDate } from '~/util/date.util';
import { isInGroupWithId } from '~/util/group.util';
import { formatForGoogleCalendar, formatForWhatsApp } from '~/util/markdown.util';
import Markdown from 'react-markdown';
import AnswerQuestionsTile from './AnswerQuestionsTile';
import toast from 'react-hot-toast';
import BorderedTile from './BorderedTile';

export default function ActivityDetailsTile({ activity, setActivity }: { activity: ActivityResponseDto; setActivity?: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>> }) {
  const { keycloak, initialized } = useKeycloak();

  const [submitting, setSubmitting] = useState(false);
  const [posterStatus, setPosterStatus] = useState<"loading" | "loaded" | "error">("loading");

  const [answers, setAnswers] = useState<Record<number, string>>({});

  const posterUrl = `${import.meta.env.ApiUrl}/api/activities/${activity.id}/poster`;
  const hasPoster = !!activity.posterFileName;

  const startDate = new Date(activity.dateTimeStart);
  const endDate = new Date(activity.dateTimeEnd);

  const startDateString = startDate.toLocaleDateString('nl-NL', {
    day: 'numeric', month: 'long', year: 'numeric'
  });

  const startTimeString = startDate.toLocaleTimeString('nl-NL', {
    hour: '2-digit', minute: '2-digit'
  });

  const endDateString = endDate.toLocaleDateString('nl-NL', {
    day: 'numeric', month: 'long', year: 'numeric'
  });

  const endTimeString = endDate.toLocaleTimeString('nl-NL', {
    hour: '2-digit', minute: '2-digit'
  });

  const isEnrolled = activity.enrollments.some(e => e.member.id === keycloak.tokenParsed?.UserId);

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

    const enrollmentProcess = async () => {
      try {
        setSubmitting(true);

        const response = await postApiEnrollments({
          body: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId,
            specificationAnswers: Object.entries(answers).map(([questionId, answer]) => ({
              questionId: Number(questionId),
              answer
            }))
          }
        });

        if (response.data) {
          const newEnrollment = {
            isOnWaitingList: response.data.isOnWaitingList,
            memberId: keycloak.tokenParsed?.UserId,
            activityId: activity.id,
            member: {
              id: keycloak.tokenParsed?.UserId,
              firstName: keycloak.tokenParsed?.given_name,
              lastName: keycloak.tokenParsed?.family_name,
              profilePicturePath: response.data.member?.profilePicturePath
            },
            specificationAnswers: response.data.specificationAnswers
          } as any;

          activity.enrollments = activity.enrollments
            ? [...activity.enrollments, newEnrollment]
            : [newEnrollment];

          setActivity && setActivity({ ...activity });
        }

      } catch (error) {
        console.error("Error while enrolling:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(enrollmentProcess(), {
      loading: t("signing_in"),
      success: t("enrollment_successful"),
      error: t("enrollment_failed")
    });
  };

  const handleUpdateEnrollment = async () => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) return;

    const updateProcess = async () => {
      try {
        setSubmitting(true);

        const response = await putApiEnrollmentsByActivityIdByMemberId({
          path: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId
          },
          body: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId,
            specificationAnswers: Object.entries(answers).map(([questionId, answer]) => ({
              questionId: Number(questionId),
              answer: String(answer)
            }))
          }
        });

        if (response.error) {
          throw new Error("Update failed");
        }

        const updatedEnrollments = activity.enrollments.map(e => {
          if (e.member.id === keycloak.tokenParsed?.UserId) {
            return {
              ...e,
              specificationAnswers: e.specificationAnswers?.map(existingAns => ({
                ...existingAns,
                answer: answers[existingAns.questionId] ?? existingAns.answer
              }))
            };
          }
          return e;
        });

        setActivity && setActivity({ ...activity, enrollments: updatedEnrollments });
      } catch (error) {
        console.error("Error while updating enrollment:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(updateProcess(), {
      loading: t("saving"),
      success: t("answers_updated"),
      error: t("update_failed")
    });
  };

  const handleUnenrollment = async () => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) {
      return;
    }

    const unenrollmentProcess = async () => {
      try {
        setSubmitting(true);
        
        await deleteApiEnrollmentsByActivityIdByMemberId({
          path: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId
          }
        });

        activity.enrollments = activity.enrollments.filter(e => e.member.id !== keycloak.tokenParsed?.UserId);
        setActivity && setActivity({ ...activity });
      } catch (error) {
        console.error("Error while unenrolling:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(unenrollmentProcess(), {
      loading: t("signing_out"),
      success: t("unenrollment_successful"),
      error: t("unenrollment_failed")
    });
  };

  const handleCopyForWhatsapp = async (lang: "NL" | "EN") => {
    const text = lang === "NL" ?
      `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Locatie: ${activity.location || 'TBA'} | Prijs: ${activity.price === 0 || activity.price == null ? 'Gratis' : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.dutchDescription)}` :
      `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Location: ${activity.location || 'TBA'} | Price: ${activity.price === 0 || activity.price == null ? 'Free' : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.englishDescription)}`;

    toast.promise(navigator.clipboard.writeText(text), {
      loading: t("copying"),
      success: t("copy_successful"),
      error: t("copy_failed")
    });
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
              <img
                src={posterUrl}
                alt={activity.name}
                onLoad={() => setPosterStatus("loaded")}
                onError={() => setPosterStatus("error")}
                className={`w-full h-full object-cover transition-opacity duration-500 ${
                  posterStatus === "loading" ? "opacity-0" : "opacity-100"
                }`}
                loading='lazy'
              />
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
            {activity.price === 0 || activity.price == null ? 'Gratis' : `€ ${activity.price.toFixed(2)}`}
          </p>
        </section>

        <BorderedTile>
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-bold text-slate-900">{t("description")}</h3>

            {isInGroupWithId(keycloak.tokenParsed, 1) && (
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
              <Markdown>
                {keycloak.tokenParsed?.locale == "NL"
                  ? activity.dutchDescription || "Geen beschrijving beschikbaar."
                  : activity.englishDescription || "No description available."}
              </Markdown>
            </div>
        </BorderedTile>

        {/* Info Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-y-6 gap-x-4 p-2">
          <InfoItem 
            icon={<Calendar size={18}/>} 
            label={t("date")} 
            value={`${startDateString} ${startTimeString} - ${startDate.toDateString() !== endDate.toDateString() ? `${endDateString} ` : ''}${endTimeString}`} 
          />
          <InfoItem icon={<MapPin size={18}/>} label={t("location")} value={activity.location || ''} />
          <InfoItem icon={<Clock size={18}/>} label={t("unenrollment_deadline")} value={activity.unenrollmentDeadline ? formatDate(new Date(activity.unenrollmentDeadline), "fullDateTime") : t("none")} />
          <InfoItem
            icon={<Clock size={18} />}
            label={t("enrollment_deadline")}
            value={
              activity.enrollmentDeadline
                ? formatDate(new Date(activity.enrollmentDeadline), "fullDateTime")
                : t("none")
            }
          />
          <InfoItem icon={<Users size={18}/>} label={t("participants")} value={`${activity.enrollments?.filter(e => !e.isOnWaitingList).length}${activity.participantLimit ? ` ${t("of")} ${activity.participantLimit}` : ''}`} />
        </div>

        <AnswerQuestionsTile
          questions={activity.specificationQuestions}
          answers={activity.enrollments.find(e => e.member.id === keycloak.tokenParsed?.UserId)?.specificationAnswers ?? []}
          onChange={(answers) => setAnswers(answers)}
          disabled={submitting || activity.enrollmentDeadline ? new Date(Date.now()) > new Date(activity.enrollmentDeadline!) : false}
        />

        {/* Actions */}
        <div className="flex flex-col gap-3 pt-4 border-t border-slate-100">
          {isEnrolled ? (
            <div className="flex flex-col gap-3">
              {(activity.specificationQuestions.length) > 0 && (
                <Button 
                  variant='primary' 
                  onClick={handleUpdateEnrollment} 
                  disabled={submitting}
                >
                  {submitting ? t("saving") : t("update_answers")}
                </Button>
              )}
              
              <Button 
                variant='danger' 
                onClick={handleUnenrollment} 
                disabled={submitting || (activity.unenrollmentDeadline ? new Date(Date.now()) > new Date(activity.unenrollmentDeadline) : false)}
              >
                {t("sign_out")}{submitting && ('...')}
              </Button>
            </div>
          ) : (
            <Button 
              variant='primary' 
              onClick={handleEnrollment} 
              disabled={submitting}
            >
              {activity.participantLimit && activity.participantLimit <= (activity.enrollments.length) ? t("sign_in_on_waitlist") : t("sign_in")}
              {submitting && ('...')}
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
import { t } from "i18next";
import { Calendar, CheckCircle, ChevronDown, Euro, MessageCircle, UserMinus } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { getApiActivities, getApiPaymentsExport, getApiPaymentsOverpaid, getApiPaymentsUnpaid, postApiPaymentsActivity, type Activity, type ActivityResponseDto, type Enrollment, type EnrollmentBalance, type Member } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import { formatDate } from "~/util/date.util";

export default function Finances() {
    const [loading, setLoading] = useState(true);
    const [exporting, setExporting] = useState(false);
    const [totalUnpaid, setTotalUnpaid] = useState(0);
    const [openPayments, setOpenPayments] = useState(0);
    const [expiredActivities, setExpiredActivities] = useState<ActivityResponseDto[] | null>(null);
    const [unpaidActivities, setUnpaidActivities] = useState<Activity[] | null>(null);
    const [membersWithOverduePayment, setMembersWithOverduePayment] = useState<{member: Member, enrollments: EnrollmentBalance[]}[] | null>(null);
    const [unpaidBalances, setUnpaidBalances] = useState<EnrollmentBalance[] | null>(null);
    const [overpaidBalances, setOverpaidBalances] = useState<EnrollmentBalance[] | null>(null);
    const [exportStartDate, setExportStartDate] = useState<string>("");
    const [exportEndDate, setExportEndDate] = useState<string>("");

    const handleWhatsAppClick = ({member, enrollments}: {member: Member, enrollments: EnrollmentBalance[]}) => {
        const unpaidEnrollments = enrollments.filter(e => 
            e.balance > 0 && 
            e.enrollment.activity && 
            new Date(e.enrollment.activity.paymentDeadline) < new Date()
        );

        const totalAmount = unpaidEnrollments.reduce((sum, e) => sum + e.balance, 0).toFixed(2);
        const activityList = unpaidEnrollments
            .map(e => `- ${e.enrollment.activity?.name} - €${e.balance.toFixed(2)}`)
            .join('\n');

        const oldestDeadline = new Date(Math.min(...unpaidEnrollments.map(e => new Date(e.enrollment.activity!.paymentDeadline).getTime())));
        const daysOverdue = Math.floor((Date.now() - oldestDeadline.getTime()) / (1000 * 60 * 60 * 24));

        let deadlineTextNL = "";
        let deadlineTextEN = "";

        if (daysOverdue > 14) {
            deadlineTextNL = "vandaag";
            deadlineTextEN = "today";
        } else if (daysOverdue > 7) {
            deadlineTextNL = "binnen 7 dagen";
            deadlineTextEN = "within 7 days";
        } else {
            deadlineTextNL = "binnen 14 dagen";
            deadlineTextEN = "within 14 days";
        }

        const isNL = member.preferredLanguage === "NL";

        const message = isNL 
            ? `Hey ${member.firstName}! De penningmeester van Sticky hier :)\n\nDe volgende activiteiten staan open:\n${activityList}\n\nDit geeft een totaal van €${totalAmount}\n\nZou je dit bedrag ${deadlineTextNL} via het betalingsportaal op Koala willen betalen voor je wordt geschorst? Deze kun je vinden via: https://koala.svsticky.nl`
            : `Hi ${member.firstName}! This is the treasurer of Sticky :)\n\nThe following activities are still unpaid:\n${activityList}\n\nThis totals €${totalAmount}\n\nWould you mind paying this ${deadlineTextEN} via the payment portal on Koala before you get suspended? You can find it here: https://koala.svsticky.nl`;

        const cleanPhone = member.phoneNumber.replace(/\D/g, '').replace(/^0/, '31');
        window.open(`https://wa.me/${cleanPhone}?text=${encodeURIComponent(message)}`, '_blank');
    };

    const handleMarkAsPaid = async (member: Member, enrollments: EnrollmentBalance[]) => {
        try{
            setLoading(true);
            const response = await postApiPaymentsActivity({
                body: {
                    memberId: member.id,
                    activityIds: enrollments.map(e => e.enrollment.activityId),
                    manuallyMarkedAsPaid: true,
                }
            });
        }
        catch(error) {
            console.error("Error while marking as paid:", error);
        } finally {
            // TO DO: Refactor to avoid duplicate code with useEffect, also use toast
            const unpaidBalances = await getApiPaymentsUnpaid({
                query: {
                    allUsers: true,
                }
            });
            if(unpaidBalances.data) {
                setUnpaidBalances(unpaidBalances.data.filter(b => b.balance !== 0));
                const totalUnpaidAmount = unpaidBalances.data.reduce((sum, payment) => sum + payment.balance, 0);
                setTotalUnpaid(totalUnpaidAmount);
                setOpenPayments(unpaidBalances.data.length);
                const activitiesWithUnpaid = unpaidBalances.data.reduce((activities: Activity[], payment) => {
                    if(payment.enrollment.activity && !activities.some(a => a.id === payment.enrollment.activity?.id)) {
                        payment.enrollment.activity && activities.push(payment.enrollment.activity);
                    }
                    return activities;
                }, []);
                setUnpaidActivities(activitiesWithUnpaid);
                
                const membersMap: Record<string, {member: Member, enrollments: EnrollmentBalance[]}> = {};
                unpaidBalances.data.forEach(payment => {
                    const member = payment.enrollment.member;
                    if(member && member.id) {
                        if(!membersMap[member.id]) {
                            membersMap[member.id] = { member, enrollments: [] };
                        }
                        membersMap[member.id].enrollments.push(payment);
                    }
                });
                setMembersWithOverduePayment(Object.values(membersMap));
            }
            setLoading(false);
        }
    };

    const handleExport = () => {
        const exportAction = async () => {
            try {
                setExporting(true);
                const response = await getApiPaymentsExport({
                    query: {
                        startDate: exportStartDate,
                        endDate: exportEndDate,
                    },
                    responseType: "blob",
                });
                
                const blob = new Blob([response.data as any], { type: "text/csv" });
                const url = window.URL.createObjectURL(blob);
                
                const link = document.createElement("a");
                link.href = url;
                link.setAttribute("download", `payments_${exportStartDate}_to_${exportEndDate}.csv`);
                document.body.appendChild(link);
                link.click();

                link.parentNode?.removeChild(link);
                window.URL.revokeObjectURL(url);
            } catch (error) {
                console.error("Error while exporting payments:", error);
            } finally {
                setExporting(false);
            }
        }
        toast.promise(exportAction(), {
            loading: t("exporting"),
            success: t("export_success"),
            error: t("export_failed"),
        });
    };

    useEffect(() => {
        const fetchData = async () => {
            try{
                setLoading(true);

                const expiredActivitiesResponse = await getApiActivities({
                    query: {
                        IncludePast: true,
                        IncludeFuture: false,
                        OpenForPayment: false,
                    }
                });
                setExpiredActivities(expiredActivitiesResponse.data || []);

                const unpaidBalances = await getApiPaymentsUnpaid({
                    query: {
                        allUsers: true,
                    }
                });
                
                if(unpaidBalances.data) {
                    setUnpaidBalances(unpaidBalances.data.filter(b => b.balance !== 0));
                    const totalUnpaidAmount = unpaidBalances.data.reduce((sum, payment) => sum + payment.balance, 0);
                    setTotalUnpaid(totalUnpaidAmount);
                    setOpenPayments(unpaidBalances.data.length);
                    const activitiesWithUnpaid = unpaidBalances.data.reduce((activities: Activity[], payment) => {
                        if(payment.enrollment.activity && !activities.some(a => a.id === payment.enrollment.activity?.id)) {
                            payment.enrollment.activity && activities.push(payment.enrollment.activity);
                        }
                        return activities;
                    }, []);
                    setUnpaidActivities(activitiesWithUnpaid);
                    
                    const membersMap: Record<string, {member: Member, enrollments: EnrollmentBalance[]}> = {};
                    unpaidBalances.data.forEach(payment => {
                        const member = payment.enrollment.member;
                        if(member && member.id) {
                            if(!membersMap[member.id]) {
                                membersMap[member.id] = { member, enrollments: [] };
                            }
                            membersMap[member.id].enrollments.push(payment);
                        }
                    });
                    setMembersWithOverduePayment(Object.values(membersMap));
                }

                const overpaidBalances = await getApiPaymentsOverpaid();
                if(overpaidBalances.data) {
                    setOverpaidBalances(overpaidBalances.data.filter(b => b.balance !== 0));
                }
            }
            catch(error) {
                console.error("Error while fetching data:", error);
            }
            finally {
                setLoading(false);
            }
        }
        fetchData();
    }, []);

    if (loading) return t("loading");

    if(totalUnpaid === null 
        || totalUnpaid === undefined 
        || openPayments === null 
        || openPayments === undefined 
        || expiredActivities === null 
        || expiredActivities === undefined) 
        return t("failed_fetching");

    return (
        <>
            <PageHeader title={t("finances")} backTo="/" action={
                <div className="flex flex-col sm:flex-row items-end gap-2">
                    <Input type="date" label={t("start_date")} name="start_date" onChange={(e: React.ChangeEvent<HTMLInputElement>) => setExportStartDate(e.target.value)} />
                    <Input type="date" label={t("end_date")} name="end_date" onChange={(e: React.ChangeEvent<HTMLInputElement>) => setExportEndDate(e.target.value)} />
                    <div className="w-full sm:w-auto">
                        <Button 
                            variant="secondary" 
                            className="w-full"
                            disabled={exportStartDate === "" || exportEndDate === "" || exporting} 
                            onClick={handleExport}
                        >
                            {t("export")}{exporting && "..."}
                        </Button>
                    </div>
                </div>
            } />
            <div className="flex flex-col gap-4 w-full"> 
                <div className="flex flex-col sm:flex-row gap-4 w-full">
                    <BorderedTile title={t("total_unpaid")} icon={Euro} className="flex-1">
                        <p className="text-2xl font-bold text-slate-800">
                            {totalUnpaid.toLocaleString(undefined, { style: "currency", currency: "EUR" })}
                        </p>
                        <p className="text-(--board-primary) text-sm font-semibold mt-1">
                            {openPayments} {t("open_payments")}
                        </p>
                    </BorderedTile>

                    <BorderedTile title={t("overpaid")} className="flex-1">
                        {overpaidBalances && overpaidBalances.length > 0 ? (
                            <div className="flex flex-col gap-2">
                                {overpaidBalances.map((balance, index) => (
                                    <>
                                        <div key={index} className="p-3 bg-green-100 rounded-lg flex items-center justify-between">
                                            <span className="text-sm text-slate-700">{balance.enrollment.member?.firstName} {balance.enrollment.member?.lastName}</span>
                                            <span className="font-bold text-green-600">{`€${Math.abs(balance.balance).toFixed(2)}`}</span>
                                            <span className="text-xs text-green-500">{balance.enrollment.activity?.name}</span>
                                        </div>
                                        <Button variant="primary" className="self-end">
                                            {t("processed")}
                                        </Button>
                                    </>
                                ))}
                            </div>
                        ) : (
                            <p className="text-sm text-slate-500">{t("no_overpaid_balances")}</p>
                        )}
                    </BorderedTile>
                </div>

                <BorderedTile title={t("expired_activities")} subtitle={t("expired_activities_subtitle")}>
                    {expiredActivities.map(activity => (
                        <Tile className="bg-gray-100 flex w-full justify-between items-center p-4 rounded-lg" key={activity.id}>
                            <div className="flex flex-col">
                                <span className="text-slate-700">{activity.name}</span>
                                <div className="flex gap-2 items-center text-sm text-slate-500">
                                    <span>{formatDate(new Date(activity.dateTimeEnd), "fullDateTime")}</span>
                                    •
                                    <span>{activity.enrollments.length} {t("participants")}</span>
                                    •
                                    <span>{`€${activity.price?.toFixed(2) || t("free")}`}</span>
                                </div>
                            </div>
                            <Button variant="primary">{t("go_to_activity")}</Button>
                        </Tile>
                    ))}
                </BorderedTile>
                
                <BorderedTile title={t("finances_activity_overview")} subtitle={t("overdue_payment_subtitle")}>
                    {unpaidActivities?.map(activity => (
                            <BorderedTile
                                key={activity.id}
                                title={activity.name}
                                className="bg-gray-100"
                                children={
                                    <div className="text-xs flex gap-2 items-center">
                                        <span className="ml-4 text-(--board-primary) font-bold">{t("outstanding")}: €{unpaidBalances?.filter(b => b.enrollment?.activityId === activity.id).reduce((sum, balance) => sum + balance.balance, 0).toFixed(2)}</span>
                                    </div>
                                }
                                
                                collapsibleContent={
                                    <div className="flex flex-col gap-3">
                                        <span className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-1">
                                            {t("unpaid_members")} ({membersWithOverduePayment?.length || 0})
                                        </span>
                                        {membersWithOverduePayment?.map((memberWithOverduePayment) => {
                                            const member = memberWithOverduePayment.member;

                                            return (
                                                <div key={member.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-3 border border-slate-100 rounded-xl gap-3">
                                                    <div className="flex items-center justify-between sm:justify-start sm:gap-6 flex-1">
                                                        <span className="font-semibold text-slate-700 text-sm">
                                                            {member.firstName} {member.lastName}
                                                        </span>
                                                        
                                                        <span className="font-bold text-slate-600 sm:ml-auto sm:mr-4">
                                                            {`€${memberWithOverduePayment.enrollments.reduce((sum, enrollment) => sum + enrollment.balance, 0).toFixed(2)}`}
                                                        </span>
                                                    </div>

                                                    <div className="w-full sm:w-auto">
                                                        <Button 
                                                            variant="primary" 
                                                            className="w-full sm:w-auto"
                                                            onClick={() => handleMarkAsPaid(member, memberWithOverduePayment.enrollments)} 
                                                            disabled={loading}
                                                        >
                                                            {t("mark_as_paid")}
                                                        </Button>
                                                    </div>
                                                </div>
                                            )
                                        })}
                                    </div>
                                }
                            />
                    ))}
                </BorderedTile>

                <BorderedTile 
                    title={t("overdue_payment")} 
                    subtitle={t("overdue_payment_subtitle")} 
                    className=""
                >
                    <div className="flex flex-col">
                        {membersWithOverduePayment?.map((memberWithOverduePayment) =>{
                            if(!memberWithOverduePayment.enrollments.some((enrollment) => enrollment.balance > 0 && enrollment.enrollment.activity && new Date(enrollment.enrollment.activity?.paymentDeadline) < new Date(Date.now()))) return null;
                            const member = memberWithOverduePayment.member;
                            const oldestDeadline = new Date(Math.min(...memberWithOverduePayment.enrollments.map(e => new Date(e.enrollment.activity?.paymentDeadline || "").getTime())));
                            const isOlderThan21Days = oldestDeadline < new Date(Date.now() - 21 * 24 * 60 * 60 * 1000);
                            
                            return (     
                                <div className={`p-3 first:rounded-t last:rounded-b border-b border-slate-100 last:border-0 ${isOlderThan21Days ? "bg-red-100" : ""}`}>
                                    <div className="grid grid-cols-1 sm:grid-cols-[1fr_auto] gap-x-4">
                                        <div className="font-semibold text-slate-700">
                                            {member.firstName} {member.lastName}
                                        </div>

                                        <div className="mt-3 sm:mt-0 row-start-3 sm:row-start-1 sm:col-start-2">
                                            <Button 
                                                variant="secondary" 
                                                className="flex flex-row items-center justify-center gap-2 w-full sm:w-auto" 
                                                onClick={() => handleWhatsAppClick(memberWithOverduePayment)}
                                            >
                                                <MessageCircle className="w-4 h-4" />
                                                WhatsApp
                                            </Button>
                                        </div>

                                        <div className="flex flex-col gap-2 mt-2 sm:col-start-1">
                                            {memberWithOverduePayment.enrollments.map((enrollment, index) => {
                                                if (enrollment.balance <= 0 || !enrollment.enrollment.activity || new Date(enrollment.enrollment.activity?.paymentDeadline) >= new Date(Date.now())) return null;
                                                return (
                                                    <div key={index} className="text-sm text-slate-600">
                                                        • {enrollment.enrollment.activity.name} - €{enrollment.balance.toFixed(2)} 
                                                        <span className="text-xs text-slate-400 ml-1">
                                                            ({t("due_date")}: {formatDate(new Date(enrollment.enrollment.activity?.paymentDeadline || ""), "shortDate")})
                                                        </span>
                                                    </div>
                                                )
                                            })}
                                        </div>
                                    </div>
                                </div>
                            )
                        })}
                    </div>
                </BorderedTile>
            </div>
        </>
    );
}
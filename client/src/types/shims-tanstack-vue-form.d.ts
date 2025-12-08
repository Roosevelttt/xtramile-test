declare module '@tanstack/vue-form' {
  export interface FormOptions<T> {
    defaultValues?: T
    onSubmit?: (args: { value: T }) => Promise<void> | void
  }
  export type FieldApi<T> = any
  export function useForm<T>(options: FormOptions<T>): any
}
